using Examine;
using Examine.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Core;
using Umbraco.Core.Composing;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Persistence.Repositories;
using Umbraco.Core.PropertyEditors;
using Umbraco.Core.Services;

namespace MemberListView.Indexing
{
    public class MemberIndexingComponent : IComponent
    {
        private readonly IExamineManager examineManager;
        private readonly ILogger logger;
        private readonly IMemberTypeService memberTypeService;
        private readonly IMemberService memberService;
        private readonly PropertyEditorCollection propertyEditors;

        public MemberIndexingComponent(IExamineManager examineManager, ILogger logger,
                                    IMemberTypeService memberTypeService, IMemberService memberService,
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
            SetupIndexTransformation(Umbraco.Core.Constants.UmbracoIndexes.MembersIndexName);
        }

        private void SetupIndexTransformation(string indexName)
        {
            if (!examineManager.TryGetIndex(indexName, out IIndex index))
            {
                logger.Warn<MemberIndexingComponent>($"No index found by the name {indexName}");
            }
            else
            {
                index.FieldDefinitionCollection.AddOrUpdate(new FieldDefinition(Umbraco.Core.Constants.Conventions.Member.Comments, FieldDefinitionTypes.FullText));

                index.FieldDefinitionCollection.AddOrUpdate(new FieldDefinition(Umbraco.Core.Constants.Conventions.Member.IsLockedOut, FieldDefinitionTypes.FullTextSortable));
                index.FieldDefinitionCollection.AddOrUpdate(new FieldDefinition(Umbraco.Core.Constants.Conventions.Member.IsApproved, FieldDefinitionTypes.FullTextSortable));
                index.FieldDefinitionCollection.AddOrUpdate(new FieldDefinition(Umbraco.Core.Constants.Conventions.Member.FailedPasswordAttempts, FieldDefinitionTypes.Integer));

                index.FieldDefinitionCollection.AddOrUpdate(new FieldDefinition(Umbraco.Core.Constants.Conventions.Member.LastLoginDate, FieldDefinitionTypes.DateTime));
                index.FieldDefinitionCollection.AddOrUpdate(new FieldDefinition(Umbraco.Core.Constants.Conventions.Member.LastLockoutDate, FieldDefinitionTypes.DateTime));
                index.FieldDefinitionCollection.AddOrUpdate(new FieldDefinition(Umbraco.Core.Constants.Conventions.Member.LastPasswordChangeDate, FieldDefinitionTypes.DateTime));

                index.FieldDefinitionCollection.AddOrUpdate(new FieldDefinition(Constants.Members.Groups, FieldDefinitionTypes.FullTextSortable));

                // Add other user-defined properties.
                //var memberTypes = memberTypeService.GetAll();
                //foreach (var memberType in memberTypes)
                //{
                //    foreach (var prop in memberType.PropertyTypes)
                //    {
                //        if (!index.FieldDefinitionCollection.Any(f => f.Name == prop.Alias))
                //        {
                //            index.FieldDefinitionCollection.AddOrUpdate(new FieldDefinition(prop.Alias, GetFieldDefinitionType(prop)));
                //        }
                //    }
                //}

                //we need to cast because BaseIndexProvider contains the TransformingIndexValues event
                if (!(index is BaseIndexProvider indexProvider))
                {
                    logger.Warn<MemberIndexingComponent>("Could not cast {index} to BaseIndexProvider", index);
                    throw new InvalidOperationException($"Could not cast {index} to BaseIndexProvider");
                }

                indexProvider.TransformingIndexValues += IndexProviderTransformingIndexValues;

                //// Add the extra fields to the ValueSetValidator.
                //if (indexProvider.ValueSetValidator is Umbraco.Examine.MemberValueSetValidator memberValueSetValidator)
                //{
                //    var includeFields = memberValueSetValidator.IncludeFields.ToList();

                //    memberValueSetValidator.IncludeFields = includeFields;
                //}
            }

        }

        private void IndexProviderTransformingIndexValues(object sender, IndexingItemEventArgs e)
        {
            //var indexProvider = sender as BaseIndexProvider;

            var memberTypes = memberTypeService.GetAll().Select(x => x.Alias);

            if (!memberTypes.InvariantContains(e.ValueSet.ItemType))
            {
                return;
            }

            // Get the groups
            AddGroups(e);
            var member = memberService.GetById(int.Parse(e.ValueSet.Id));
            IndexMembershipFields(e, member);

        }

        private void AddGroups(IndexingItemEventArgs e)
        {
            var groups = memberService.GetAllRoles(int.Parse(e.ValueSet.Id)).Aggregate("", (list, group) => string.IsNullOrEmpty(list) ? group : $"{list} {group}");
            e.ValueSet.Add(Constants.Members.Groups, groups);
        }

        /// <summary>
        /// Make sure the isApproved and isLockedOut fields are setup properly in the index
        /// </summary>
        /// <param name="e"></param>
        /// <param name="member"></param>
        /// <remarks>
        ///  these fields are not consistently updated in the XML fragment when a member is saved (as they may never get set) so we have to do this.
        ///  </remarks>
        private void IndexMembershipFields(IndexingItemEventArgs e, IMember member)
        {
            if (!e.ValueSet.Values.ContainsKey(Umbraco.Core.Constants.Conventions.Member.IsLockedOut))
            {
                e.ValueSet.Add(Umbraco.Core.Constants.Conventions.Member.IsLockedOut, member.IsLockedOut.ToString());
            }

            if (!e.ValueSet.Values.ContainsKey(Umbraco.Core.Constants.Conventions.Member.IsApproved))
            {
                e.ValueSet.Add(Umbraco.Core.Constants.Conventions.Member.IsApproved, member.IsApproved.ToString());
            }

            var values = new Dictionary<string, IEnumerable<object>>();
            foreach (var property in member.Properties)
            {
                AddPropertyValue(property, null, null, values);
            }

            foreach(var value in values)
            {
                if (e.ValueSet.Values.ContainsKey(value.Key))
                {
                    continue;
                }
                var val = value.Value.FirstOrDefault();
                if (val != null)
                {
                    e.ValueSet.Add(value.Key, val);
                }
            }

        }
        protected void AddPropertyValue(Property property, string culture, string segment, IDictionary<string, IEnumerable<object>> values)
        {
            var editor = propertyEditors[property.PropertyType.PropertyEditorAlias];
            if (editor == null) return;

            var indexVals = editor.PropertyIndexValueFactory.GetIndexValues(property, culture, segment, false);
            foreach (var keyVal in indexVals)
            {
                if (keyVal.Key.IsNullOrWhiteSpace()) continue;

                var cultureSuffix = culture == null ? string.Empty : "_" + culture;

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
                                if (values.TryGetValue(key, out var v))
                                    values[key] = new List<object>(v) { val }.ToArray();
                                else
                                    values.Add($"{keyVal.Key}{cultureSuffix}", val.Yield());
                            }
                            break;
                        default:
                            {
                                var key = $"{keyVal.Key}{cultureSuffix}";
                                if (values.TryGetValue(key, out var v))
                                    values[key] = new List<object>(v) { val }.ToArray();
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

        private string GetFieldDefinitionType(PropertyType prop)
        {
            switch (prop.PropertyEditorAlias)
            {
                case Umbraco.Core.Constants.PropertyEditors.Aliases.Boolean:
                case Umbraco.Core.Constants.PropertyEditors.Aliases.Slider:
                    return FieldDefinitionTypes.Integer;
                case Umbraco.Core.Constants.PropertyEditors.Aliases.Integer:
                    return FieldDefinitionTypes.Long;
                case Umbraco.Core.Constants.PropertyEditors.Aliases.Decimal:
                    return FieldDefinitionTypes.Double;
                case Umbraco.Core.Constants.PropertyEditors.Aliases.DateTime:
                case Umbraco.Core.Constants.PropertyEditors.Legacy.Aliases.Date:
                    return FieldDefinitionTypes.DateTime;
                case Umbraco.Core.Constants.PropertyEditors.Aliases.EmailAddress:
                    return FieldDefinitionTypes.EmailAddress;
                default:
                    return FieldDefinitionTypes.FullText;
            }
        }
    }
}