/**
 * @ngdoc service
 * @name umbraco.resources.memberListViewResource
 * 
 * @description Member Management
 * 
 * @returns {umbraco.resources.memberListViewResource} memberListViewResource
 *
 * @param {any} $http Http Service
 * @param {any} umbRequestHelper Umbraco Request Helper
 **/
function memberListViewResource($http, umbRequestHelper) {
    "use strict";
    var memberListViewResource = {
        approveByKey: function (key) {
            if (!key) {
                throw "key cannot be null"
            }

            return umbRequestHelper.resourcePromise(
                $http.post(umbRequestHelper.getApiUrl(
                    "memberListViewBaseUrl",
                    "ApproveByKey",
                    [{ key: key }])),
                'Failed to approve member with key ' + key)
        },

        suspendByKey: function (key) {
            if (!key) {
                throw "key cannot be null"
            }

            return umbRequestHelper.resourcePromise(
                $http.post(umbRequestHelper.getApiUrl(
                    "memberListViewBaseUrl",
                    "SuspendByKey",
                    [{ key: key }])),
                'Failed to approve member with key ' + key)
        },

        unlockByKey: function (key) {
            if (!key) {
                throw "key cannot be null"
            }

            return umbRequestHelper.resourcePromise(
                $http.post(umbRequestHelper.getApiUrl(
                    "memberListViewBaseUrl",
                    "UnlockByKey",
                    [{ key: key }])),
                'Failed to approve member with key ' + key)
        },

        getPagedResults: function (options) {
            var defaults = {
                pageSize: 0,
                pageNumber: 0,
                filterData: { filter: null },
                orderDirection: "Ascending",
                orderBy: "SortOrder",
                orderBySystemField: true
            }

            if (options === undefined) {
                options = {}
            }
            //overwrite the defaults if there are any specified
            angular.extend(defaults, options)
            //now copy back to the options we will use
            options = defaults
            //change asc/desc
            if (options.orderDirection === "asc") {
                options.orderDirection = "Ascending"
            }
            else if (options.orderDirection === "desc") {
                options.orderDirection = "Descending"
            }

            // Create the params dictionary
            var params = filterToDictionary(options.filterData)
            params.push({ pageNumber: options.pageNumber })
            params.push({ pageSize: options.pageSize })
            params.push({ orderBy: options.orderBy })
            params.push({ orderDirection: options.orderDirection })
            params.push({ orderBySystemField: toBool(options.orderBySystemField) })

            return umbRequestHelper.resourcePromise(
                $http.get(
                    umbRequestHelper.getApiUrl(
                        "memberListViewBaseUrl",
                        "GetPagedMembers",
                        params)),
                'Failed to retrieve member paged result')
        },

        getExport: function (options) {

            var defaults = {
                filterData: { filter: null },
                orderDirection: "Ascending",
                orderBy: "SortOrder",
                format: "Excel",
                orderBySystemField: true,
                columns: null,
                ids: null
            }
            if (options === undefined) {
                options = {}
            }
            //overwrite the defaults if there are any specified
            angular.extend(defaults, options)

            //now copy back to the options we will use
            options = defaults

            //change asc/desc
            if (options.orderDirection === "asc") {
                options.orderDirection = "Ascending"
            }
            else if (options.orderDirection === "desc") {
                options.orderDirection = "Descending"
            }

            // Create the params dictionary
            var params = filterToDictionary(options.filterData)
            params.push({ orderBy: options.orderBy })
            params.push({ orderDirection: options.orderDirection })
            params.push({ orderBySystemField: toBool(options.orderBySystemField) })
            params.push({ format: options.format })

            if (options.columns !== null && options.columns !== undefined && options.columns.length > 0) {
                params.push({ columns: options.columns })
            }

            if (options.ids !== null && options.ids !== undefined && options.ids.length > 0) {
                params.push({ ids: options.ids })
            }

            var config = { responseType: 'blob' }
            const url = umbRequestHelper.getApiUrl(
                "memberListViewBaseUrl",
                "GetExportedMembers",
                params)

            $http.get(url, config).then(function (response) {
                    _startBlobDownload(response)

                }, function (data) {
                    console.log(data)
                })
        },

        getMemberGroups: function () {
            return umbRequestHelper.resourcePromise(
                $http.get(umbRequestHelper.getApiUrl(
                    "memberListViewBaseUrl",
                    "GetMemberGroups")),
                'Failed to retrieve groups')
        },

        getMemberColumns: function (memberType) {
            var params = []
            if (memberType === undefined || memberType === null) {
                params.push({ memberType: memberType })
            }
            return umbRequestHelper.resourcePromise(
                $http.get(umbRequestHelper.getApiUrl(
                    "memberListViewBaseUrl",
                    "GetMemberColumns",
                    params)),
                'Failed to retrieve groups')
        },

        canExport: function () {
            return umbRequestHelper.resourcePromise(
                $http.get(umbRequestHelper.getApiUrl(
                    "memberListViewBaseUrl",
                    "GetCanExport")),
                false)
        }
    }

    //converts the value to a js bool
    function toBool(v) {
        if (typeof v === 'number') {
            return v > 0
        }
        if (typeof v === 'string') {
            return v === "true"
        }
        if (typeof v === "boolean") {
            return v
        }
        return false;
    }

    function filterToDictionary(filter) {
        if (!filter)
            return
        var dict = []

        for (var prop in filter) {
            if (filter.hasOwnProperty(prop) &&
                filter[prop] &&
                (
                    (prop.startsWith('f_') ||
                        prop === 'filter' ||
                        prop === 'memberType' ||
                        prop === 'memberGroups') &&
                    filter[prop].length > 0
                ) ||
                (
                    (prop === 'umbracoMemberApproved' ||
                        prop === 'umbracoMemberLockedOut') &&
                    filter[prop] !== null
                )
            ) {

                // Add a new dictionary entry.
                var entry = {}
                entry[prop] = filter[prop]
                dict.push(entry)

            }
        }

        return dict

    }

    function _startBlobDownload(response) {
        const headers = response.headers()

        var filename = headers['x-filename']

        if (!filename) {
            var result = headers['content-disposition'].split(';')[1].trim().split('=')[1]
            filename = result.replace(/"/g, '')
        }

        //var contentType = headers['content-type']
        if (window.navigator && window.navigator.msSaveOrOpenBlob) {
            // for IE
            window.navigator.msSaveOrOpenBlob(response.data, filename);
        } else {
            // for Non-IE (chrome, firefox etc.)
            var urlObject = URL.createObjectURL(response.data);

            var downloadLink = angular.element('<a>Download</a>');
            downloadLink.css('display', 'none');
            downloadLink.attr('href', urlObject);
            downloadLink.attr('download', filename);
            angular.element(document.body).append(downloadLink);
            downloadLink[0].click();

            // cleanup
            downloadLink.remove();
            URL.revokeObjectURL(urlObject);
        }
    }

    return memberListViewResource
}

angular.module('umbraco.resources').factory('memberListViewResource', memberListViewResource)
