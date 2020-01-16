/**
 * @ngdoc service
 * @name umbraco.resources.memberExtResource
 * 
 * @description Member Management
 * 
 * @returns {umbraco.resources.memberExtResource} memberExtResource
 *
 * @param {any} $http Http Service
 * @param {any} $window Window
 * @param {any} umbRequestHelper Umbraco Request Helper
 **/
function memberExtResource($http, $window, umbRequestHelper) {
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

            var config = { responseType: 'blob' };

            // Solution taken from http://jaliyaudagedara.blogspot.com/2016/05/angularjs-download-files-by-sending.html
            $http.get("Backoffice/MemberManager/MemberApi/GetMembersExport?" + umbRequestHelper.dictionaryToQueryString(querystring),
                config).success(function (data, status, headers) {
                    headers = headers();
                    try {
                        var filename = headers['x-filename'];

                        if (!filename) {
                            var result = headers['content-disposition'].split(';')[1].trim().split('=')[1];
                            filename = result.replace(/"/g, '');
                        }

                        var contentType = headers['content-type'];

                        var linkElement = document.createElement('a');

                        var blob = new Blob([data], { type: contentType });
                        var url = window.URL.createObjectURL(blob);

                        linkElement.setAttribute('href', url);
                        linkElement.setAttribute("download", filename);

                        var clickEvent = new MouseEvent("click", {
                            "view": window,
                            "bubbles": true,
                            "cancelable": false
                        });
                        linkElement.dispatchEvent(clickEvent);
                    } catch (ex) {
                        console.log(ex);
                    }

                }).error(function (data) {
                    console.log(data);
                });
        }
    };

    function _filterToDictionary(filter) {
        if (!filter)
            return;
        var dict = [];

        for (prop in filter) {
            if (filter.hasOwnProperty(prop) &&
                filter[prop] &&
                (prop.startsWith('f_') || prop === 'filter' || prop === 'memberType') &&
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
