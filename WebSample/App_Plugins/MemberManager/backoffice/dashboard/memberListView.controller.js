/**
 * @ngdoc controller
 * @name MemberManager.List.ViewController
 * @function
 * 
 * @description
 * The controller for the member list view
 * 
 * @param {object} $scope Scope
 * @param {object} $routeParams Route Parameters
 * @param {object} $timeout Timeout
 * @param {object} $location Location
 * @param {object} memberResource Member Resource
 * @param {object} memberExtResource Member Extension Resource (custom)
 * @param {object} memberTypeResource Member Type Resource
 * @param {object} notificationsService Notifications Service
 * @param {object} iconHelper Icon Helper
 * @param {object} dialogService Dialog Service
 * @param {object} editorState Editor State
 * @param {object} localizationService Localisation Service
 * @param {object} listViewHelper Listview Helper
 */
function memberListViewController($scope, $routeParams, $timeout, $location, memberResource, memberExtResource, memberTypeResource, notificationsService, iconHelper, dialogService, editorState, localizationService, listViewHelper) {

    var deleteItemCallback, getIdCallback, createEditUrlCallback;

    $scope.model = {};
    $scope.model.config = {
        pageSize: 100,
        orderBy: 'Name',
        orderDirection: 'asc',
        includeProperties: [
            { alias: 'email', header: 'Email', isSystem: 1 },
            { alias: 'memberGroups', header: 'Group', isSystem: 1 },
            { alias: 'isApproved', header: 'Approved', isSystem: 1 },
            { alias: 'isLockedOut', header: 'Locked Out', isSystem: 1 }
        ],
        layouts: [
            { name: 'List', path: 'views/propertyeditors/listview/layouts/list/list.html', icon: 'icon-list', isSystem: 1, selected: true }
        ],
        bulkActionPermissions: {
            allowBulkDelete: true
        }
    };

    $scope.currentNodePermissions = null;

    if (editorState.current) {
        //Fetch current node allowed actions for the current user
        //This is the current node & not each individual child node in the list
        var currentUserPermissions = editorState.current.allowedActions;

        //Create a nicer model rather than the funky & hard to remember permissions strings
        $scope.currentNodePermissions = {
            "canCopy": _.contains(currentUserPermissions, 'O'), //Magic Char = O
            "canCreate": _.contains(currentUserPermissions, 'C'), //Magic Char = C
            "canDelete": _.contains(currentUserPermissions, 'D'), //Magic Char = D
            "canMove": _.contains(currentUserPermissions, 'M'), //Magic Char = M                
            "canPublish": _.contains(currentUserPermissions, 'U'), //Magic Char = U
            "canUnpublish": _.contains(currentUserPermissions, 'U') //Magic Char = Z (however UI says it can't be set, so if we can publish 'U' we can unpublish)
        };
    }

    $scope.entityType = "member";
    $scope.memberGroups = null;

    getContentTypesCallback = memberTypeResource.getTypes;
    deleteItemCallback = memberResource.deleteByKey;
    getIdCallback = function (selected) {
        return selected.key;
    };
    createEditUrlCallback = function (item) {
        return "/" + $scope.entityType + "/" + $scope.entityType + "/edit/" + item.key;
    };

    $scope.pagination = [];
    $scope.isNew = false;
    $scope.actionInProgress = false;
    $scope.selection = [];
    $scope.folders = [];
    $scope.listViewResultSet = {
        totalPages: 0,
        items: []
    };

    $scope.createAllowedButtonSingle = false;
    $scope.createAllowedButtonMulti = false;

    $scope.options = {
        pageSize: $scope.model.config.pageSize ? $scope.model.config.pageSize : 10,
        pageNumber: $routeParams.page && !isNaN($routeParams.page) && Number($routeParams.page) > 0 ? $routeParams.page : 1,
        filterData: {
            filter: null
        },
        orderBy: ($scope.model.config.orderBy ? $scope.model.config.orderBy : 'Name').trim(),
        orderDirection: $scope.model.config.orderDirection ? $scope.model.config.orderDirection.trim() : "asc",
        orderBySystemField: true,
        includeProperties: $scope.model.config.includeProperties ? $scope.model.config.includeProperties : [
            { alias: 'updateDate', header: 'Last edited', isSystem: 1 },
            { alias: 'updater', header: 'Last edited by', isSystem: 1 }
        ],
        layout: {
            layouts: $scope.model.config.layouts,
            activeLayout: listViewHelper.getLayout($routeParams.id, $scope.model.config.layouts)
        },
        allowBulkDelete: $scope.model.config.bulkActionPermissions.allowBulkDelete
    };

    // Check if selected order by field is actually custom field
    for (var j = 0; j < $scope.options.includeProperties.length; j++) {
        var includedProperty = $scope.options.includeProperties[j];
        if (includedProperty.alias.toLowerCase() === $scope.options.orderBy.toLowerCase()) {
            $scope.options.orderBySystemField = includedProperty.isSystem === 1;
            break;
        }
    }

    //update all of the system includeProperties to enable sorting
    _.each($scope.options.includeProperties, function (e, i) {

        //NOTE: special case for contentTypeAlias, it's a system property that cannot be sorted
        // to do that, we'd need to update the base query for content to include the content type alias column
        // which requires another join and would be slower. BUT We are doing this for members so not sure it makes a diff?
        if (e.alias !== "contentTypeAlias") {
            e.allowSorting = true;
        }

        if (e.isSystem) {
            //localize the header
            var key = getLocalizedKey(e.alias);
            localizationService.localize(key).then(function (v) {
                e.header = v;
            });
        }
    });

    function showNotificationsAndReset(err, reload, successMsg) {

        //check if response is ysod
        if (err.status && err.status >= 500) {

            // Open ysod overlay
            $scope.ysodOverlay = {
                view: "ysod",
                error: err,
                show: true
            };
        }

        $timeout(function () {
            $scope.bulkStatus = "";
            $scope.actionInProgress = false;
        },
            500);

        if (reload === true) {
            $scope.reloadView();
        }

        if (err.data && angular.isArray(err.data.notifications)) {
            for (var i = 0; i < err.data.notifications.length; i++) {
                notificationsService.showNotification(err.data.notifications[i]);
            }
        } else if (successMsg) {
            localizationService.localize("bulk_done")
                .then(function (v) {
                    notificationsService.success(v, successMsg);
                });
        }
    }

    $scope.next = function (pageNumber) {
        $scope.options.pageNumber = pageNumber;
        $scope.reloadView();
    };

    $scope.goToPage = function (pageNumber) {
        $scope.options.pageNumber = pageNumber;
        $scope.reloadView();
    };

    $scope.prev = function (pageNumber) {
        $scope.options.pageNumber = pageNumber;
        $scope.getContent();
    };

    $scope.isSortDirection = function (col, direction) {
        return $scope.options.orderBy.toUpperCase() === col.toUpperCase() && $scope.options.orderDirection === direction;
    };

    $scope.sort = function (field) {

        $scope.options.orderBy = field;

        if ($scope.options.orderDirection === "desc") {
            $scope.options.orderDirection = "asc";
        } else {
            $scope.options.orderDirection = "desc";
        }

        $scope.reloadView();
    };

    $scope.getContent = function (contentId) {
        $scope.reloadView();
    };

    /*Loads the search results, based on parameters set in prev,next,sort and so on*/
    /*Pagination is done by an array of objects, due angularJS's funky way of monitoring state
    with simple values */

    $scope.reloadView = function () {
        $scope.viewLoaded = false;
        $scope.folders = [];

        listViewHelper.clearSelection($scope.listViewResultSet.items, $scope.folders, $scope.selection);

        memberExtResource.getMembers($scope.options).then(function (data) {

            $scope.actionInProgress = false;
            $scope.listViewResultSet = data;

            //update all values for display
            if ($scope.listViewResultSet.items) {
                _.each($scope.listViewResultSet.items, function (e, index) {
                    setPropertyValues(e);
                });
            }

            $scope.viewLoaded = true;

            //NOTE: This might occur if we are requesting a higher page number than what is actually available, for example
            // if you have more than one page and you delete all items on the last page. In this case, we need to reset to the last
            // available page and then re-load again
            if ($scope.options.pageNumber > $scope.listViewResultSet.totalPages) {
                $scope.options.pageNumber = $scope.listViewResultSet.totalPages;

                //reload!
                $scope.reloadView();
            }

        });

    };

    var searchListView = _.debounce(function () {
        $scope.$apply(function () {
            makeSearch();
        });
    }, 500);

    $scope.forceSearch = function (ev) {
        //13: enter
        switch (ev.keyCode) {
            case 13:
                makeSearch();
                break;
        }
    };

    $scope.enterSearch = function () {
        $scope.viewLoaded = false;
        searchListView();
    };

    function makeSearch() {
        if ($scope.options.filterData.filter !== null && $scope.options.filterData.filter !== undefined) {
            $scope.options.pageNumber = 1;
            $scope.getContent();
        }
    }

    $scope.isAnythingSelected = function () {
        if ($scope.selection.length === 0) {
            return false;
        } else {
            return true;
        }
    };

    $scope.selectedItemsCount = function () {
        return $scope.selection.length;
    };

    $scope.clearSelection = function () {
        listViewHelper.clearSelection($scope.listViewResultSet.items, $scope.folders, $scope.selection);
    };

    $scope.getIcon = function (entry) {
        return iconHelper.convertFromLegacyIcon(entry.icon);
    };

    function serial(selected, fn, getStatusMsg, index) {
        return fn(selected, index).then(function (content) {
            index++;
            $scope.bulkStatus = getStatusMsg(index, selected.length);
            return index < selected.length ? serial(selected, fn, getStatusMsg, index) : content;
        }, function (err) {
            var reload = index > 0;
            showNotificationsAndReset(err, reload);
            return err;
        });
    }

    function applySelected(fn, getStatusMsg, getSuccessMsg, confirmMsg) {
        var selected = $scope.selection;
        if (selected.length === 0)
            return;
        if (confirmMsg && !confirm(confirmMsg))
            return;

        $scope.actionInProgress = true;
        $scope.bulkStatus = getStatusMsg(0, selected.length);

        return serial(selected, fn, getStatusMsg, 0).then(function (result) {
            // executes once the whole selection has been processed
            // in case of an error (caught by serial), result will be the error
            if (!(result.data && angular.isArray(result.data.notifications)))
                showNotificationsAndReset(result, true, getSuccessMsg(selected.length));
        });
    }

    function getCustomPropertyValue(alias, properties) {
        var value = '';
        var index = 0;
        var foundAlias = false;
        for (var i = 0; i < properties.length; i++) {
            if (properties[i].alias === alias) {
                foundAlias = true;
                break;
            }
            index++;
        }

        if (foundAlias) {
            value = properties[index].value;
        }

        return value;
    }

    /** This ensures that the correct value is set for each item in a row, we don't want to call a function during interpolation or ng-bind as performance is really bad that way */

    function setPropertyValues(result) {

        //set the edit url
        result.editPath = createEditUrlCallback(result);

        _.each($scope.options.includeProperties, function (e, i) {

            var alias = e.alias;

            // First try to pull the value directly from the alias (e.g. updatedBy)
            var value = result[alias];

            // If we have IsApproved or IsLocked, then customise the display.
            if (alias === "isLockedOut") {
                value = getLockedDescription(value);
            }
            if (alias === "isApproved") {
                value = getSuspendedDescription(value);
            }

            // if we have an array, then concat all values separated by comma
            if (Array.isArray(value) && value.length > 0) {
                value = _.reduce(value, function (memo, val) { return memo + ', ' + val; });
            }

            // If this returns an object, look for the name property of that (e.g. owner.name)
            if (value === Object(value)) {
                value = value['name'];
            }

            // If we've got nothing yet, look at a user defined property
            if (typeof value === 'undefined') {
                value = getCustomPropertyValue(alias, result.properties);
            }

            // If we have a date, format it
            if (isDate(value)) {
                value = value.substring(0, value.length - 3);
            }

            // set what we've got on the result
            result[alias] = value;
        });


    }

    function isDate(val) {
        if (angular.isString(val)) {
            return val.match(/^(\d{4})\-(\d{2})\-(\d{2})\ (\d{2})\:(\d{2})\:(\d{2})$/);
        }
        return false;
    }

    function initView() {
        //default to root id if the id is undefined
        var id = $routeParams.id;
        if (id === undefined) {
            id = -1;
        }

        memberExtResource.getMemberGroups().then(function (groups) {
            $scope.memberGroups = groups;
        });

        getContentTypesCallback(id).then(function (listViewAllowedTypes) {
            $scope.listViewAllowedTypes = listViewAllowedTypes;
            $scope.createAllowedButtonSingle = listViewAllowedTypes.length === 1;
            $scope.createAllowedButtonMulti = listViewAllowedTypes.length > 1;
        });

        $scope.contentId = id;
        $scope.isTrashed = id === "-20" || id === "-21";

        $scope.options.bulkActionsAllowed = $scope.options.allowBulkDelete;

        $scope.getContent();
    }

    function getLocalizedKey(alias) {

        switch (alias) {
            case "updateDate":
                return "content_updateDate";
            case "createDate":
                return "content_createDate";
            case "contentTypeAlias":
                return "content_membertype";
            case "email":
                return "general_email";
            case "username":
                return "general_username";
            case "memberGroups":
                return "general_memberGroups";
        }
        return alias;
    }

    $scope.createBlank = function (entityType, docTypeAlias) {
        $location
            .path("/" + entityType + "/" + entityType + "/edit/")
            .search("doctype=" + docTypeAlias + "&create=true");
    };

    $scope.delete = function () {
        var confirmDeleteText = "";

        localizationService.localize("defaultdialogs_confirmdelete")
            .then(function (value) {
                confirmDeleteText = value;

                var attempt =
                    applySelected(
                        function (selected, index) { return deleteItemCallback(getIdCallback(selected[index])); },
                        function (count, total) {
                            var key = (total === 1 ? "bulk_deletedItemOfItem" : "bulk_deletedItemOfItems");
                            return localizationService.localize(key, [count, total]);
                        },
                        function (total) {
                            var key = (total === 1 ? "bulk_deletedItem" : "bulk_deletedItems");
                            return localizationService.localize(key, [total]);
                        },
                        confirmDeleteText + "?");
            });
    };

    $scope.canUnlock = function () {
        if (!angular.isArray($scope.selection)) {
            return false;
        }
        return _.some($scope.selection, function (item) {
            return !isUnlocked(item);
        });

    };

    $scope.canApprove = function () {
        if (!angular.isArray($scope.selection)) {
            return false;
        }
        return _.some($scope.selection, function (item) {
            return isSuspended(item);
        });

    };

    $scope.canSuspend = function () {
        if (!angular.isArray($scope.selection)) {
            return false;
        }
        return _.some($scope.selection, function (item) {
            return isApproved(item);
        });

    };

    isSuspended = function (item) {
        return _.some($scope.listViewResultSet.items, function (member) {
            if (member.key === item.key)
                return member.isApproved === 'Suspended';
            return false;
        });
    };

    isApproved = function (item) {
        return _.some($scope.listViewResultSet.items, function (member) {
            if (member.key === item.key)
                return member.isApproved === 'Approved';
            return false;
        });
    };

    isUnlocked = function (item) {
        return _.some($scope.listViewResultSet.items, function (member) {
            if (member.key === item.key)
                return member.isLockedOut === 'Unlocked';
            return false;
        });
    };

    getLockedIcon = function (locked) {
        return locked ? 'icon-lock color-red' : 'icon-unlocked color-green';
    };

    getSuspendedIcon = function (approved) {
        return approved ? 'icon-check color-green' : 'icon-block color-red';
    };

    getLockedDescription = function (locked) {
        return locked ? 'Locked Out' : 'Unlocked';
    };

    getSuspendedDescription = function (approved) {
        return approved ? 'Approved' : 'Suspended';
    };

    $scope.export = function () {
        if ($scope.listViewResultSet.totalRecords > 100) {
            localizationService.localize("memberManager_confirmExport")
                .then(function (value) {
                    if (value && !confirm(value + "?"))
                        return;
                    memberExtResource.getMemberExport($scope.options);
                });
        } else {
            memberExtResource.getMemberExport($scope.options);
        }
    };

    $scope.unlock = function () {
        var confirmUnlockText = "";

        localizationService.localize("memberManager_confirmUnlock")
            .then(function (value) {
                confirmUnlockText = value;

                let tmpSelected = $scope.selected;
                $scope.selected = _.filter(tmpSelected, function (item) { return isUnlocked(item); });
                var attempt =
                    applySelected(
                        function (selected, index) { return memberExtResource.unlockById(getIdCallback(selected[index])); },
                        function (count, total) {
                            var key = total === 1 ? "bulk_unlockItemOfItem" : "bulk_unlockItemOfItems";
                            return localizationService.localize(key, [count, total]);
                        },
                        function (total) {
                            var key = total === 1 ? "bulk_unlockedItem" : "bulk_unlockedItems";
                            return localizationService.localize(key, [total]);
                        },
                        confirmUnlockText + "?");
                if (attempt) {
                    attempt.then(function () {
                        $timeout($scope.getContent, 1000);
                    });
                }
            });
    };

    $scope.approve = function () {
        var confirmApproveText = "";

        localizationService.localize("memberManager_confirmApprove")
            .then(function (value) {
                confirmApproveText = value;

                let tmpSelected = $scope.selected;
                $scope.selected = _.filter(tmpSelected, function (item) { return isApproved(item); });

                var attempt =
                    applySelected(
                        function (selected, index) { return memberExtResource.approveById(getIdCallback(selected[index])); },
                        function (count, total) {
                            var key = total === 1 ? "bulk_approveItemOfItem" : "bulk_approveItemOfItems";
                            return localizationService.localize(key, [count, total]);
                        },
                        function (total) {
                            var key = total === 1 ? "bulk_approvedItem" : "bulk_approvedItems";
                            return localizationService.localize(key, [total]);
                        },
                        confirmApproveText + "?");
                if (attempt) {
                    attempt.then(function () {
                        $timeout($scope.getContent, 1000);
                    });
                }
            });
    };

    $scope.suspend = function () {
        var confirmSuspendText = "";

        let tmpSelected = $scope.selected;
        $scope.selected = _.filter(tmpSelected, function (item) { return isSuspended(item); });

        localizationService.localize("memberManager_confirmSuspend")
            .then(function (value) {
                confirmSuspendText = value;

                var attempt =
                    applySelected(
                        function (selected, index) { return memberExtResource.suspendById(getIdCallback(selected[index])); },
                        function (count, total) {
                            var key = total === 1 ? "bulk_suspendItemOfItem" : "bulk_suspendItemOfItems";
                            return localizationService.localize(key, [count, total]);
                        },
                        function (total) {
                            var key = total === 1 ? "bulk_suspendedItem" : "bulk_suspendedItems";
                            return localizationService.localize(key, [total]);
                        },
                        confirmSuspendText + "?");
                if (attempt) {
                    attempt.then(function () {
                        $timeout($scope.getContent, 1000);
                    });
                }
            });
    };

    $scope.editMember = function (key) {
        dialogService.closeAll();
        dialogService.open({
            template: '/app_plugins/MemberManager/backoffice/dialogs/member/edit.html',
            key: key,
            closeOnSave: true,
            //tabFilter: ["Generic properties"],
            callback: function (data) {
                $scope.getContent();
            }
        });
    };

    $scope.clearFilter = function () {
        dialogService.closeAll();
        $scope.options.filterData = {
            filter: null
        };
        //$scope.searchDisplay = null;
        $scope.getContent();
    };

    $scope.edit = function () {
        $scope.filterDialog = {};
        $scope.filterDialog.title = localizationService.localize("memberManager_filter");
        $scope.filterDialog.section = $scope.entityType;
        $scope.filterDialog.currentNode = $scope.contentId;
        $scope.filterDialog.view = "move";
        $scope.filterDialog.show = true;

        $scope.moveDialog.submit = function (model) {

            if (model.target) {
                performMove(model.target);
            }

            $scope.moveDialog.show = false;
            $scope.moveDialog = null;
        };

        $scope.moveDialog.close = function (oldModel) {
            $scope.moveDialog.show = false;
            $scope.moveDialog = null;
        };

    };


    $scope.filter = function () {
        dialogService.closeAll();
        dialogService.open({
            template: '/app_plugins/MemberManager/backoffice/dialogs/member/filter.html',
            closeOnSave: false,
            filterData: $scope.options.filterData,
            memberTypes: $scope.listViewAllowedTypes,
            memberGroups: $scope.memberGroups,
            callback: function (data) {
                $scope.options.filterData = data;
                $scope.getContent();
            }
        });
    };
    initView();
}

angular.module("umbraco").controller("MemberManager.Dashboard.MemberListViewController", memberListViewController);