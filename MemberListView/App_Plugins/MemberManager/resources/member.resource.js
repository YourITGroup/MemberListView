/**
    * @ngdoc service
    * @name umbraco.resources.memberExtResource
    * @description Member Management
    **/
function memberExtResource($q, $http, umbDataFormatter, umbRequestHelper) {
    var memberExtResource = {
        approveById: function (id) {
            if (!id) {
                throw "id cannot be null";
            }

            return umbRequestHelper.resourcePromise(
                           $http.post("Backoffice/MemberManager/MemberApi/PostApprove?" +
                                umbRequestHelper.dictionaryToQueryString(
                                   [{ id: id }])),
                           'Failed to approve member with id ' + id);
        },
        suspendById: function (id) {
            if (!id) {
                throw "id cannot be null";
            }

            return umbRequestHelper.resourcePromise(
                           $http.post("Backoffice/MemberManager/MemberApi/PostSuspend?" +
                                umbRequestHelper.dictionaryToQueryString(
                                   [{ id: id }])),
                           'Failed to approve member with id ' + id);
        },
        unlockById: function (id) {
            if (!id) {
                throw "id cannot be null";
            }

            return umbRequestHelper.resourcePromise(
                           $http.post("Backoffice/MemberManager/MemberApi/PostUnlock?" +
                                umbRequestHelper.dictionaryToQueryString(
                                   [{ id: id }])),
                           'Failed to approve member with id ' + id);
        },
        getMembers: function (options) {

            var defaults = {
                pageSize: 0,
                pageNumber: 0,
                filter: '',
                orderDirection: "Ascending",
                orderBy: "SortOrder"
            };
            if (options === undefined) {
                options = {};
            }
            //overwrite the defaults if there are any specified
            angular.extend(defaults, options);
            //now copy back to the options we will use
            options = defaults;
            //change asc/desc
            if (options.orderDirection === "asc") {
                options.orderDirection = "Ascending";
            }
            else if (options.orderDirection === "desc") {
                options.orderDirection = "Descending";
            }

            // Create the querystring dictionary
            var querystring = _filterToDictionary(options.filter);
            querystring.push({ pageNumber: options.pageNumber });
            querystring.push({ pageSize: options.pageSize });
            querystring.push({ orderBy: options.orderBy });
            querystring.push({ orderDirection: options.orderDirection });

            return umbRequestHelper.resourcePromise(
               $http.get("Backoffice/MemberManager/MemberApi/GetMembers?" +
                    umbRequestHelper.dictionaryToQueryString(querystring)),
               'Failed to retrieve members');
        },
        getMemberExport: function (options) {

            var defaults = {
                filter: '',
                orderDirection: "Ascending",
                orderBy: "SortOrder"
            };
            if (options === undefined) {
                options = {};
            }
            //overwrite the defaults if there are any specified
            angular.extend(defaults, options);
            //now copy back to the options we will use
            options = defaults;

            //change asc/desc
            if (options.orderDirection === "asc") {
                options.orderDirection = "Ascending";
            }
            else if (options.orderDirection === "desc") {
                options.orderDirection = "Descending";
            }

            // Create the querystring dictionary
            var querystring = _filterToDictionary(options.filter);
            querystring.push({ orderBy: options.orderBy });
            querystring.push({ orderDirection: options.orderDirection });

            // using windows.location.href instead of windows.open doesn't open new a window, even temporarily.
            $window.location.href("Backoffice/MemberManager/MemberApi/GetMembersExport?" +
                    umbRequestHelper.dictionaryToQueryString(querystring));
        },
    };

    function _filterToDictionary(filter) {
        if (!filter)
            return;
        var dict = [];

        for (prop in filter) {
            if (filter.hasOwnProperty(prop) &&
                filter[prop] &&
                (prop.startsWith('f_') || prop == 'filter' || prop == 'memberType') &&
                filter[prop].length > 0) {

                // Add a new dictionary entry.
                var entry = {};
                entry[prop] = filter[prop];
                dict.push(entry);

            }
        }

        return dict;

    }

    return memberExtResource;
}

angular.module('umbraco.resources').factory('memberExtResource', memberExtResource);
