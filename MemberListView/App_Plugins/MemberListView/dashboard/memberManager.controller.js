function memberListViewController($scope, $interpolate, $routeParams, $timeout, $location, memberResource, memberExtResource, memberTypeResource, notificationsService, iconHelper, localizationService, listViewHelper, overlayService, editorService, eventsService) {
    "use strict";

    // We are specifically targeting Members.
    $scope.entityType = "member"
    const contentResource = memberResource
    const getContentTypesCallback = memberTypeResource.getTypes
    const getListResultsCallback = memberExtResource.getPagedResults
    const deleteItemCallback = contentResource.deleteByKey
    const getIdCallback = function (selected) {
        return selected.key
    }

    let labels = {}

    localizationService.localizeMany([
        getLocalizedKey('suspended'),
        getLocalizedKey('approved'),
        getLocalizedKey('lockedOut'),
        getLocalizedKey('unlocked'),
    ]
    ).then(function (data) {
        labels.suspendedLabel = data[0]
        labels.approvedLabel = data[1]
        labels.lockedOutLabel = data[2]
        labels.unlockedLabel = data[3]
    })

    $scope.model = {}
    $scope.model.config = {
        pageSize: 100,
        orderBy: 'email',
        orderDirection: 'asc',
        includeProperties: [
            { alias: 'email', header: 'Email', isSystem: 1 },
            { alias: 'memberGroups', header: 'Groups', isSystem: 0 },
//            { alias: 'firstName', header: 'First Name', isSystem: 0 },
//            { alias: 'lastName', header: 'Last Name', isSystem: 0 },
//            { alias: 'phoneNumber', header: 'Phone', isSystem: 0 },
            { alias: 'isApproved', header: 'Approved', isSystem: 0 },
            { alias: 'isLockedOut', header: 'Locked Out', isSystem: 0 }
        ],
        layouts: [
            { name: 'List', path: '/App_Plugins/MemberListView/layouts/list/list.html', icon: 'icon-list', isSystem: 1, selected: true }
        ],
        bulkActionPermissions: {
            allowBulkDelete: true,
            allowExport: true,
            allowSuspend: true,
            allowUnlock: true,
            allowApprove: true
        }
    }

    $scope.memberGroups = null

    $scope.pagination = []
    $scope.isNew = false
    $scope.actionInProgress = false
    $scope.selection = []
    $scope.folders = []
    $scope.listViewResultSet = {
        totalPages: 0,
        items: []
    }

    $scope.createAllowedButtonSingle = false
    $scope.createAllowedButtonMulti = false

    //when this is null, we don't check permissions
    $scope.currentNodePermissions = null

    //when this is null, we don't check permissions
    $scope.buttonPermissions = null


    $scope.options = {
        pageSize: $scope.model.config.pageSize ? $scope.model.config.pageSize : 10,
        pageNumber: $routeParams.page && !isNaN($routeParams.page) && Number($routeParams.page) > 0 ? $routeParams.page : 1,
        filterData: {
            filter: $routeParams.filter ? $routeParams.filter : '',
            umbracoMemberApproved: null,
            umbracoMemberLockedOut: null,

        },
        orderBy: ($routeParams.orderBy ? $routeParams.orderBy : $scope.model.config.orderBy ? $scope.model.config.orderBy : 'email').trim(),
        orderDirection: ($routeParams.orderDirection ? $routeParams.orderDirection : $scope.model.config.orderDirection ? $scope.model.config.orderDirection : "desc").trim(),
        orderBySystemField: true,
        includeProperties: $scope.model.config.includeProperties ? $scope.model.config.includeProperties : [
            { alias: 'updateDate', header: 'Last edited', isSystem: 1 },
            { alias: 'updater', header: 'Last edited by', isSystem: 1 }
        ],
        layout: {
            layouts: $scope.model.config.layouts,
            activeLayout: listViewHelper.getLayout($routeParams.id, $scope.model.config.layouts)
        },
        allowBulkDelete: $scope.model.config.bulkActionPermissions.allowBulkDelete,
        allowExport: $scope.model.config.bulkActionPermissions.allowExport,
        allowSuspend: $scope.model.config.bulkActionPermissions.allowSuspend,
        allowApprove: $scope.model.config.bulkActionPermissions.allowApprove,
        allowUnlock: $scope.model.config.bulkActionPermissions.allowUnlock,
        cultureName: $routeParams.cculture ? $routeParams.cculture : $routeParams.mculture
    }

    _.each($scope.options.includeProperties, function (property) {
        property.nameExp = !!property.nameTemplate
            ? $interpolate(property.nameTemplate)
            : undefined
    })

    //watch for culture changes in the query strings and update accordingly
    $scope.$watch(function () {
        return $routeParams.cculture ? $routeParams.cculture : $routeParams.mculture
    }, function (newVal, oldVal) {
        if (newVal && newVal !== oldVal) {
            //update the options
            $scope.options.cultureName = newVal
            $scope.reloadView($scope.contentId)
        }
    })

    // Check if selected order by field is actually custom field
    for (var j = 0; j < $scope.options.includeProperties.length; j++) {
        var includedProperty = $scope.options.includeProperties[j]
        if (includedProperty.alias.toLowerCase() === $scope.options.orderBy.toLowerCase()) {
            $scope.options.orderBySystemField = includedProperty.isSystem === 1
            break
        }
    }

    //update all of the system includeProperties to enable sorting
    _.each($scope.options.includeProperties, function (e, i) {
        e.allowSorting = true

        // Special case for members, only the configured system fields should be enabled sorting
        // (see MemberRepository.ApplySystemOrdering)
        if (e.isSystem && $scope.entityType === "member") {
            e.allowSorting = e.alias === "username" ||
                e.alias === "email" ||
                e.alias === "updateDate" ||
                e.alias === "createDate" ||
                e.alias === "contentTypeAlias"
        }

        //localize the header if we can
        var key = getLocalizedKey(e.alias)
        if (key !== e.alias) {
            localizationService.localize(key).then(function (v) {
                e.header = v
            })
        }
    })

    $scope.selectLayout = function (layout) {
        $scope.options.layout.activeLayout = listViewHelper.setLayout($routeParams.id, layout, $scope.model.config.layouts)
    }

    function showNotificationsAndReset(err, reload, successMsg) {

        //check if response is ysod
        if (err.status && err.status >= 500) {

            // Open ysod overlay
            $scope.ysodOverlay = {
                view: "ysod",
                error: err,
                show: true
            }
        }

        $timeout(function () {
            $scope.bulkStatus = ""
            $scope.actionInProgress = false
        },
            500)

        if (reload === true) {
            $scope.reloadView()
        }

        if (err.data && angular.isArray(err.data.notifications)) {
            for (var i = 0; i < err.data.notifications.length; i++) {
                notificationsService.showNotification(err.data.notifications[i])
            }
        } else if (successMsg) {
            localizationService.localize("bulk_done")
                .then(function (v) {
                    notificationsService.success(v, successMsg)
                })
        }
    }

    $scope.next = function (pageNumber) {
        $scope.options.pageNumber = pageNumber
        $scope.reloadView()
    }

    $scope.goToPage = function (pageNumber) {
        $scope.options.pageNumber = pageNumber
        $scope.reloadView()
    }

    $scope.prev = function (pageNumber) {
        $scope.options.pageNumber = pageNumber
        $scope.getContent()
    }

    $scope.isSortDirection = function (col, direction) {
        return $scope.options.orderBy.toUpperCase() === col.toUpperCase() && $scope.options.orderDirection === direction
    }

    /*Loads the search results, based on parameters set in prev,next,sort and so on*/
    /*Pagination is done by an array of objects, due angularJS's funky way of monitoring state
    with simple values */
    $scope.getContent = function (contentId) {
        $scope.reloadView()
    }

    $scope.reloadView = function () {
        $scope.viewLoaded = false
        $scope.folders = []

        listViewHelper.clearSelection($scope.listViewResultSet.items, $scope.folders, $scope.selection)

        getListResultsCallback($scope.options).then(function (data) {

            $scope.actionInProgress = false
            $scope.listViewResultSet = data

            //update all values for display
            if ($scope.listViewResultSet.items) {
                _.each($scope.listViewResultSet.items, function (e, index) {
                    setPropertyValues(e)
                })
            }

            $scope.viewLoaded = true

            //NOTE: This might occur if we are requesting a higher page number than what is actually available, for example
            // if you have more than one page and you delete all items on the last page. In this case, we need to reset to the last
            // available page and then re-load again
            if ($scope.options.pageNumber > $scope.listViewResultSet.totalPages) {
                $scope.options.pageNumber = $scope.listViewResultSet.totalPages

                //reload!
                $scope.reloadView()
            }

        })

    }

    $scope.makeSearch = function() {
        if ($scope.options.filterData.filter !== null && $scope.options.filterData.filter !== undefined) {
            $scope.options.pageNumber = 1
            $scope.getContent()
        }
    }

    $scope.onSearchStartTyping = function () {
        $scope.viewLoaded = false
    }

    $scope.selectedItemsCount = function () {
        return $scope.selection.length
    }

    $scope.clearSelection = function () {
        listViewHelper.clearSelection($scope.listViewResultSet.items, $scope.folders, $scope.selection)
    }

    $scope.getIcon = function (entry) {
        return iconHelper.convertFromLegacyIcon(entry.icon)
    }

    function serial(selected, fn, getStatusMsg, index) {
        return fn(selected, index).then(function (content) {
            index++
            $scope.bulkStatus = getStatusMsg(index, selected.length)
            return index < selected.length ? serial(selected, fn, getStatusMsg, index) : content
        }, function (err) {
            var reload = index > 0
            showNotificationsAndReset(err, reload)
            return err
        })
    }

    function applySelected(fn, getStatusMsg, getSuccessMsg, confirmMsg) {
        var selected = $scope.selection
        if (selected.length === 0)
            return
        if (confirmMsg && !confirm(confirmMsg))
            return

        $scope.actionInProgress = true
        $scope.bulkStatus = getStatusMsg(0, selected.length)

        return serial(selected, fn, getStatusMsg, 0).then(function (result) {
            // executes once the whole selection has been processed
            // in case of an error (caught by serial), result will be the error
            if (!(result.data && angular.isArray(result.data.notifications)))
                showNotificationsAndReset(result, true, getSuccessMsg(selected.length))
        })
    }

    $scope.delete = function () {

        const dialog = {
            view: "views/propertyeditors/listview/overlays/delete.html",
            deletesVariants: selectionHasVariants(),
            isTrashed: $scope.isTrashed,
            submitButtonLabelKey: "contentTypeEditor_yesDelete",
            submitButtonStyle: "danger",
            submit: function (model) {
                performDelete()
                overlayService.close()
            },
            close: function () {
                overlayService.close()
            }
        }

        localizationService.localize("general_delete").then(value => {
            dialog.title = value
            overlayService.open(dialog)
        })

    }

    function performDelete() {
        applySelected(
            function (selected, index) { return deleteItemCallback(getIdCallback(selected[index])) },
            function (count, total) {
                var key = (total === 1 ? "bulk_deletedItemOfItem" : "bulk_deletedItemOfItems")
                return localizationService.localize(key, [count, total])
            },
            function (total) {
                var key = (total === 1 ? "bulk_deletedItem" : "bulk_deletedItems")
                return localizationService.localize(key, [total])
            }).then(function () {
                $scope.reloadView($scope.contentId, true)
            })
    }


    $scope.canUnlock = function () {
        if (!angular.isArray($scope.selection)) {
            return false
        }
        return _.some($scope.selection, function (item) {
            return !isUnlocked(item)
        })

    }

    $scope.canApprove = function () {
        if (!angular.isArray($scope.selection)) {
            return false
        }
        return _.some($scope.selection, function (item) {
            return isSuspended(item)
        })

    }

    $scope.canSuspend = function () {
        if (!angular.isArray($scope.selection)) {
            return false
        }
        return _.some($scope.selection, function (item) {
            return isApproved(item)
        })

    }

    function isSuspended (item) {
        return _.some($scope.listViewResultSet.items, function (member) {
            if (member.key === item.key)
                return member.isApproved === labels['suspendedLabel']
            return false
        })
    }

    function isApproved (item) {
        return _.some($scope.listViewResultSet.items, function (member) {
            if (member.key === item.key)
                return member.isApproved === labels['approvedLabel']
            return false
        })
    }

    function isUnlocked (item) {
        return _.some($scope.listViewResultSet.items, function (member) {
            if (member.key === item.key)
                return member.isLockedOut === labels['unlockedLabel']
            return false
        })
    }

    //function getLockedIcon (locked) {
    //    return locked ? 'icon-lock color-red' : 'icon-unlocked color-green'
    //}

    //function getSuspendedIcon (approved) {
    //    return approved ? 'icon-check color-green' : 'icon-block color-red'
    //}

    function getLockedDescription (locked) {
        var key = locked ? 'lockedOut' : 'unlocked'
        return labels[key + 'Label']
    }

    function getSuspendedDescription (approved) {
        var key = approved ? 'approved' : 'suspended'
        return labels[key + 'Label']
    }

    $scope.exportRecords = function () {
        const dialog = {
            view: "/app_plugins/MemberListView/dialogs/member/export.html",
            submitButtonLabelKey: "memberManager_exportMembers",
            size: "small",
            filterData: $scope.options.filterData,
            memberTypes: $scope.listViewAllowedTypes,
            memberGroups: $scope.memberGroups,
            totalItems: $scope.listViewResultSet.totalItems,
            columns: $scope.options.columns,
            format: $scope.options.format,
            submit: function (model) {
                $scope.options.columns = model.columns
                $scope.options.format = model.format
                memberExtResource.getExport($scope.options)
                editorService.close()
            },
            close: function () {
                editorService.close()
            }
        }

        localizationService.localize("memberManager_export").then(value => {
            dialog.title = value
            editorService.open(dialog)
        })
    }

    $scope.unlock = function () {
        const dialog = {
            view: "/App_Plugins/MemberListView/overlays/action.html",
            actionKey: "memberManager_confirmUnlock",
            submitButtonLabelKey: "memberManager_yesUnlock",
            //            submitButtonStyle: "danger",
            submit: function (model) {
                performUnlock()
                overlayService.close()
            },
            close: function () {
                overlayService.close()
            }
        }

        localizationService.localize("actions_unlock").then(value => {
            dialog.title = value
            overlayService.open(dialog)
        })
    }

    function performUnlock() {
        let tmpSelected = $scope.selected
        $scope.selected = _.filter(tmpSelected, function (item) { return isLockedOut(item) })

        applySelected(
            function (selected, index) { return memberExtResource.unlockByKey(getIdCallback(selected[index])) },
            function (count, total) {
                var key = total === 1 ? "bulk_unlockItemOfItem" : "bulk_unlockItemOfItems"
                return localizationService.localize(key, [count, total])
            },
            function (total) {
                var key = total === 1 ? "bulk_unlockedItem" : "bulk_unlockedItems"
                return localizationService.localize(key, [total])
            }).then(function () {
                $scope.reloadView($scope.contentId, true)
            })
    }
    $scope.approve = function () {
        const dialog = {
            view: "/App_Plugins/MemberListView/overlays/action.html",
            actionKey: "memberManager_confirmApprove",
            submitButtonLabelKey: "memberManager_yesApprove",
//            submitButtonStyle: "danger",
            submit: function (model) {
                performApprove()
                overlayService.close()
            },
            close: function () {
                overlayService.close()
            }
        }

        localizationService.localize("actions_approve").then(value => {
            dialog.title = value
            overlayService.open(dialog)
        })
    }

    function performApprove() {
        let tmpSelected = $scope.selected
        $scope.selected = _.filter(tmpSelected, function (item) { return isSuspended(item) })

        applySelected(
            function (selected, index) { return memberExtResource.approveByKey(getIdCallback(selected[index])) },
            function (count, total) {
                var key = total === 1 ? "bulk_approveItemOfItem" : "bulk_approveItemOfItems"
                return localizationService.localize(key, [count, total])
            },
            function (total) {
                var key = total === 1 ? "bulk_approvedItem" : "bulk_approvedItems"
                return localizationService.localize(key, [total])
            }).then(function () {
                $scope.reloadView($scope.contentId, true)
            })
    }

    $scope.suspend = function () {
        const dialog = {
            view: "/App_Plugins/MemberListView/overlays/action.html",
            actionKey: "memberManager_confirmSuspend",
            submitButtonLabelKey: "memberManager_yesSuspend",
            submitButtonStyle: "danger",
            submit: function (model) {
                performSuspend()
                overlayService.close()
            },
            close: function () {
                overlayService.close()
            }
        }

        localizationService.localize("actions_suspend").then(value => {
            dialog.title = value
            overlayService.open(dialog)
        })

    }

    function performSuspend() {
        let tmpSelected = $scope.selected
        $scope.selected = _.filter(tmpSelected, function (item) { return !isSuspended(item) })

        applySelected(
            function (selected, index) { return memberExtResource.suspendByKey(getIdCallback(selected[index])) },
            function (count, total) {
                var key = total === 1 ? "bulk_suspendItemOfItem" : "bulk_suspendItemOfItems"
                return localizationService.localize(key, [count, total])
            },
            function (total) {
                var key = total === 1 ? "bulk_suspendedItem" : "bulk_suspendedItems"
                return localizationService.localize(key, [total])
            }).then(function () {
                $scope.reloadView($scope.contentId, true)
            })
    }

    $scope.clearFilter = function () {
        $scope.options.filterData = {
            filter: null
        }
        $scope.getContent()
    }

    $scope.filter = function () {
        const dialog = {
            view: "/app_plugins/MemberListView/dialogs/member/filter.html",
            submitButtonLabelKey: "memberManager_applyFilter",
            size: "small",
            filterData: $scope.options.filterData,
            memberTypes: $scope.listViewAllowedTypes,
            memberGroups: $scope.memberGroups,
            submit: function (data) {
                $scope.options.filterData = data
                $scope.getContent()
                editorService.close()
            },
            close: function () {
                editorService.close()
            }
        }

        localizationService.localize("memberManager_filter").then(value => {
            dialog.title = value
            editorService.open(dialog)
        })

    }

    function selectionHasVariants() {
        let variesByCulture = false

        // check if any of the selected nodes has variants
        $scope.selection.forEach(selectedItem => {
            $scope.listViewResultSet.items.forEach(resultItem => {
                if ((selectedItem.id === resultItem.id || selectedItem.key === resultItem.key) && resultItem.variesByCulture) {
                    variesByCulture = true
                }
            })
        })

        return variesByCulture
    }

    function getCustomPropertyValue(alias, properties) {
        var value = ''
        var index = 0
        var foundAlias = false
        for (var i = 0; i < properties.length; i++) {
            if (properties[i].alias === alias) {
                foundAlias = true
                break
            }
            index++
        }

        if (foundAlias) {
            value = properties[index].value
        }

        return value
    }

    /** This ensures that the correct value is set for each item in a row, we don't want to call a function during interpolation or ng-bind as performance is really bad that way */

    function setPropertyValues(result) {

        //set the edit url
        //result.editPath = createEditUrlCallback(result)

        _.each($scope.options.includeProperties, function (e, i) {

            var alias = e.alias

            // First try to pull the value directly from the alias (e.g. updatedBy)
            var value = result[alias]

            // If we have IsApproved or IsLocked, then customise the display.
            if (alias === "isLockedOut") {
                value = getLockedDescription(value)
            }
            if (alias === "isApproved") {
                value = getSuspendedDescription(value)
            }

            // if we have an array, then concat all values separated by comma
            if (Array.isArray(value) && value.length > 0) {
                value = _.reduce(value, function (memo, val) { return memo + ', ' + val })
            }

            // If this returns an object, look for the name property of that (e.g. owner.name)
            if (value === Object(value)) {
                value = value['name']
            }

            // If we've got nothing yet, look at a user defined property
            if (typeof value === 'undefined') {
                value = getCustomPropertyValue(alias, result.properties)
            }

            // If we have a date, format it
            if (isDate(value)) {
                value = value.substring(0, value.length - 3)
            }

            // set what we've got on the result
            result[alias] = value
        })


    }

    function isDate(val) {
        if (angular.isString(val)) {
            return val.match(/^(\d{4})\-(\d{2})\-(\d{2})\ (\d{2})\:(\d{2})\:(\d{2})$/)
        }
        return false
    }

    function initView() {
        //default to root id if the id is undefined
        var id = $routeParams.id
        if (id === undefined) {
            id = 'all-members' 
        }

        memberExtResource.getMemberGroups().then(function (groups) {
            $scope.memberGroups = groups
        })

        getContentTypesCallback(id).then(function (listViewAllowedTypes) {
            $scope.listViewAllowedTypes = listViewAllowedTypes
            $scope.createAllowedButtonSingle = listViewAllowedTypes.length === 1
            $scope.createAllowedButtonMulti = listViewAllowedTypes.length > 1
        })

        $scope.contentId = id
        $scope.isTrashed = id === "-20" || id === "-21"

        if ($scope.options.allowExport) {
            memberExtResource.canExport().then(function (result) {
                $scope.options.allowExport = result
            })
        }
            
        $scope.options.bulkActionsAllowed = $scope.options.allowBulkDelete ||
            $scope.options.allowSuspend ||
            $scope.options.allowUnlock ||
            $scope.options.allowApprove ||
            $scope.options.allowExport

        $scope.reloadView()
    }

    function getLocalizedKey(alias) {

        switch (alias) {
            case "updateDate":
                return "content_updateDate"
            case "createDate":
                return "content_createDate"
            case "contentTypeAlias":
                return "content_membertype"
            case "email":
                return "general_email"
            case "username":
                return "general_username"
            case "memberGroups":
                return "general_memberGroups"
            case "suspended":
                return "memberManager_suspended"
            case "approved":
                return "memberManager_approved"
            case "lockedOut":
                return "memberManager_lockedOut"
            case "isApproved":
                return "memberManager_approved"
            case "isLockedOut":
                return "memberManager_lockedOut"
            case "unlocked":
                return "memberManager_unlocked"
        }
        return alias
    }

    function createBlank(entityType, docTypeAlias) {
        $location
            .path("/" + entityType + "/" + entityType + "/edit/" + $scope.contentId)
            .search("doctype", docTypeAlias)
            .search("create", "true")
    }

    function toggleDropdown() {
        $scope.page.createDropdownOpen = !$scope.page.createDropdownOpen
    }

    function leaveDropdown() {
        $scope.page.createDropdownOpen = false
    }

    $scope.createBlank = createBlank
    $scope.toggleDropdown = toggleDropdown
    $scope.leaveDropdown = leaveDropdown


    //// if this listview has sort order in it, make sure it is updated when sorting is performed on the current content
    //if (_.find($scope.options.includeProperties, property => property.alias === "sortOrder")) {
    //    var eventSubscription = eventsService.on("sortCompleted", function (e, args) {
    //        if (parseInt(args.id) === parseInt($scope.contentId)) {
    //            $scope.reloadView($scope.contentId)
    //        }
    //    })

    //    $scope.$on('$destroy', function () {
    //        eventsService.unsubscribe(eventSubscription)
    //    })
    //}

    initView()
}

angular.module("umbraco").controller("MemberListView.Dashboard.MemberManagerController", memberListViewController)