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
function memberExtResource($http, umbRequestHelper) {
    var memberExtResource = {
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
                columns: null
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
            params.push({ columns: options.columns })

            var config = { responseType: 'blob' }

            // Solution taken from http://jaliyaudagedara.blogspot.com/2016/05/angularjs-download-files-by-sending.html
            $http.get(
                umbRequestHelper.getApiUrl(
                    "memberListViewBaseUrl",
                    "GetExportedMembers",
                    params),
                config).then(function (response) {
                    headers = response.headers()
                    try {
                        var filename = headers['x-filename']

                        if (!filename) {
                            var result = headers['content-disposition'].split(';')[1].trim().split('=')[1]
                            filename = result.replace(/"/g, '')
                        }

                        var contentType = headers['content-type']

                        var linkElement = document.createElement('a')

                        var blob = new Blob([response.data], { type: contentType })
                        var url = window.URL.createObjectURL(blob)

                        linkElement.setAttribute('href', url)
                        linkElement.setAttribute("download", filename)

                        var clickEvent = new MouseEvent("click", {
                            "view": window,
                            "bubbles": true,
                            "cancelable": false
                        })
                        linkElement.dispatchEvent(clickEvent)
                    } catch (ex) {
                        console.log(ex)
                    }

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
        if (Utilities.isNumber(v)) {
            return v > 0
        }
        if (Utilities.isString(v)) {
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

        for (prop in filter) {
            if (filter.hasOwnProperty(prop) &&
                filter[prop] &&
                (prop.startsWith('f_') ||
                    prop === 'filter' ||
                    prop === 'memberType' ||
                    prop === 'memberGroups' ||
                    prop === 'umbracoMemberApproved' ||
                    prop === 'umbracoMemberLockedOut') &&
                filter[prop].length > 0) {

                // Add a new dictionary entry.
                var entry = {}
                entry[prop] = filter[prop]
                dict.push(entry)

            }
        }

        return dict

    }

    return memberExtResource
}

angular.module('umbraco.resources').factory('memberExtResource', memberExtResource)
