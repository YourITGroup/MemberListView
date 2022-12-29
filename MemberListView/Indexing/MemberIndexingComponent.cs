using Examine;
using Examine.Lucene.Providers;
using MemberListView.Extensions;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;
using static Umbraco.Cms.Core.Constants;

namespace MemberListView.Indexing
{
    public class MemberIndexingComponent : IComponent
    {
        private readonly IExamineManager examineManager;
        private readonly ILogger<MemberIndexingComponent> logger;
        private readonly IMemberTypeService memberTypeService;
        private readonly IMemberService memberService;
        private readonly PropertyEditorCollection propertyEditors;

        public MemberIndexingComponent(IExamineManager examineManager,
                                       ILogger<MemberIndexingComponent> logger,
                                       IMemberTypeService memberTypeService,
                                       IMemberService memberService,
                                       PropertyEditorCollection propertyEditors)
        {
            this.examineManager = examineManager;
            this.logger = logger;
            this.memberTypeService = memberTypeService;
            this.memberService = memberService;
            this.propertyEditors = propertyEditors;
        }

        public void Initialize()
        {
            SetupIndexTransformation(UmbracoIndexes.MembersIndexName);
        }

        private void SetupIndexTransformation(string indexName)
        {
            if (!examineManager.TryGetIndex(indexName, out IIndex index))
            {
                logger.LogWarning("No index found by the name {indexName}", indexName);
                return;
            }

            if (index is LuceneIndex luceneIndex)
            {
                luceneIndex.TransformingIndexValues += LuceneIndex_TransformingIndexValues;
                luceneIndex.IndexingError += LuceneIndex_IndexingError;
            }
        }

        private void LuceneIndex_IndexingError(object? sender, IndexingErrorEventArgs e)
        {
            logger.LogWarning(e.Exception, "Error occurred during indexing: {message}", e.Message);
        }

        private void LuceneIndex_TransformingIndexValues(object? sender, IndexingItemEventArgs e)
        {
            var memberTypes = memberTypeService.GetAll().Select(x => x.Alias);

            if (!memberTypes.InvariantContains(e.ValueSet.ItemType))
            {
                return;
            }

            var updatedValues = e.ValueSet.Values.ToDictionary(x => x.Key, x => x.Value.ToList());

            // Get the groups
            AddGroups(e, updatedValues);
            var member = memberService.GetById(int.Parse(e.ValueSet.Id));
            if (member is not null)
            {
                IndexMembershipFields(e, updatedValues, member);
            }
            e.SetValues(updatedValues.ToDictionary(x => x.Key, x => (IEnumerable<object>)x.Value));
        }

        private void AddGroups(IndexingItemEventArgs e, Dictionary<string, List<object>> updatedValues)
        {
            var groups = memberService.GetAllRoles(int.Parse(e.ValueSet.Id)).Aggregate("", (list, group) => string.IsNullOrEmpty(list) ? group : $"{list} {group}");
            updatedValues.Add(Constants.Members.Groups, groups.GetObjList());
        }

        /// <summary>
        /// Make sure the isApproved and isLockedOut fields are setup properly in the index
        /// </summary>
        /// <param name="e"></param>
        /// <param name="updatedValues"></param>
        /// <param name="member"></param>
        /// <remarks>
        ///  these fields are not consistently updated in the XML fragment when a member is saved (as they may never get set) so we have to do this.
        ///  </remarks>
        private void IndexMembershipFields(IndexingItemEventArgs e, Dictionary<string, List<object>> updatedValues, IMember member)
        {
            if (!e.ValueSet.Values.ContainsKey(nameof(IMember.IsLockedOut)))
            {
                updatedValues.Add(nameof(IMember.IsLockedOut), member.IsLockedOut.ToString().GetObjList());
            }

            if (!e.ValueSet.Values.ContainsKey(nameof(IMember.IsApproved)))
            {
                updatedValues.Add(nameof(IMember.IsApproved), member.IsApproved.ToString().GetObjList());
            }

            if (!e.ValueSet.Values.ContainsKey(nameof(IMember.FailedPasswordAttempts)))
            {
                updatedValues.Add(nameof(IMember.FailedPasswordAttempts), member.FailedPasswordAttempts.GetObjList());
            }

            if (!e.ValueSet.Values.ContainsKey(nameof(IMember.LastLoginDate)) && member.LastLoginDate is not null)
            {
                updatedValues.Add(nameof(IMember.LastLoginDate), member.LastLoginDate.GetObjList());
            }

            if (!e.ValueSet.Values.ContainsKey(nameof(IMember.LastLockoutDate)) && member.LastLockoutDate is not null)
            {
                updatedValues.Add(nameof(IMember.LastLockoutDate), member.LastLockoutDate.GetObjList());
            }

            if (!e.ValueSet.Values.ContainsKey(nameof(IMember.LastPasswordChangeDate)) && member.LastPasswordChangeDate is not null)
            {
                updatedValues.Add(nameof(IMember.LastPasswordChangeDate), member.LastPasswordChangeDate.GetObjList());
            }

            var values = new Dictionary<string, IEnumerable<object>>();
            foreach (var property in member.Properties)
            {
                AddPropertyValue(property, null, null, values);
            }

            foreach (var value in values)
            {
                if (e.ValueSet.Values.ContainsKey(value.Key))
                {
                    continue;
                }
                var val = value.Value.FirstOrDefault();
                if (val != null)
                {
                    updatedValues.Add(value.Key, val.GetObjList());
                }
            }
        }

        protected void AddPropertyValue(IProperty property, string? culture, string? segment, IDictionary<string, IEnumerable<object>> values)
        {
            var editor = propertyEditors[property.PropertyType.PropertyEditorAlias];
            if (editor == null) return;

            var indexVals = editor.PropertyIndexValueFactory.GetIndexValues(property, culture, segment, false);
            foreach (var keyVal in indexVals)
            {
                if (keyVal.Key.IsNullOrWhiteSpace()) continue;

                var cultureSuffix = culture == null ? string.Empty : $"_{culture}";

                foreach (var val in keyVal.Value)
                {
                    switch (val)
                    {
                        //only add the value if its not null or empty (we'll check for string explicitly here too)
                        case null:
                            continue;
                        case string strVal:
                            {
                                if (strVal.IsNullOrWhiteSpace()) continue;
                                var key = $"{keyVal.Key}{cultureSuffix}";
                                if (values.TryGetValue(key, out _))
                                    values[key] = val.Yield();
                                else
                                    values.Add($"{keyVal.Key}{cultureSuffix}", val.Yield());
                            }
                            break;
                        default:
                            {
                                var key = $"{keyVal.Key}{cultureSuffix}";
                                if (values.TryGetValue(key, out _))
                                    values[key] = val.Yield();
                                else
                                    values.Add($"{keyVal.Key}{cultureSuffix}", val.Yield());
                            }

                            break;
                    }
                }
            }
        }

        public void Terminate()
        {
        }
    }
}