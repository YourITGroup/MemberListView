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
 * @param {object} angularHelper Angular Helper
 * @param {object} memberResource Member Resource
 * @param {object} memberExtResource Member Extension Resource (custom)
 * @param {object} memberTypeResource Member Type Resource
 * @param {object} notificationsService Notifications Service
 * @param {object} iconHelper Icon Helper
 * @param {object} dialogService Dialog Service
 */
function memberListViewController($scope, $routeParams, $timeout, angularHelper, memberResource, memberExtResource, memberTypeResource, notificationsService, iconHelper, dialogService) {
    var vm = this;

    vm.pagination = [];
    vm.isNew = false;
    vm.actionInProgress = false;
    vm.listViewAllowedTypes = [];
    vm.listViewResultSet = {
        totalPages: 0,
        items: []
    };

    vm.searchDisplay = null;

    vm.filterButtonGroup = {
        defaultButton: {
            alias: "filter",
            labelKey: "memberManager_filter",
            hotKey: "ctrl+f",
            handler: function () {
                vm.filterMembers();
            }
        },
        subButtons: [
        ]
    };

    vm.options = {
        pageSize: 10,
        pageNumber: $routeParams.page && !isNaN($routeParams.page) && Number($routeParams.page) > 0 ? $routeParams.page : 1,
        filter: {},
        orderBy: 'Name',
        orderDirection: "asc"
        //orderBy: (vm.model.config.orderBy ? vm.model.config.orderBy : 'Name').trim(),
        //orderDirection: vm.model.config.orderDirection ? vm.model.config.orderDirection.trim() : "asc"
    };

    vm.isSortDirection = function (col, direction) {
        return vm.options.orderBy.toUpperCase() === col.toUpperCase() && vm.options.orderDirection === direction;
    };

    vm.next = function () {
        if (vm.options.pageNumber < vm.listViewResultSet.totalPages) {
            vm.options.pageNumber++;
            reloadView();
        }
    };

    vm.goToPage = function (pageNumber) {
        vm.options.pageNumber = pageNumber + 1;
        reloadView();
    };

    vm.sort = function (field) {

        vm.options.orderBy = field;

        if (vm.options.orderDirection === "desc") {
            vm.options.orderDirection = "asc";
        } else {
            vm.options.orderDirection = "desc";
        }

        reloadView();
    };

    vm.prev = function () {
        if (vm.options.pageNumber > 1) {
            vm.options.pageNumber--;
            reloadView();
        }
    };

    buildFilterButtons = function () {
        if (vm.searchDisplay !== null) {
            vm.filterButtonGroup.subButtons = [
                {
                    alias: "clearFilter",
                    labelKey: "memberManager_clearFilter",
                    hotKey: "ctrl+shift+f",
                    handler: function () {
                        vm.clearFilter();
                    }
                },
                {
                    alias: "exportMembers",
                    labelKey: "memberManager_exportMembers",
                    hotKey: "ctrl+e",
                    handler: function () {
                        vm.exportFiltered();
                    }
                }
            ];
        } else {
            vm.filterButtonGroup.subButtons = [
                {
                    alias: "exportMembers",
                    labelKey: "memberManager_exportMembers",
                    hotKey: "ctrl+e",
                    handler: function () {
                        vm.exportFiltered();
                    }
                }
            ];
        }
    };

    /*Loads the search results, based on parameters set in prev,next,sort and so on*/
    /*Pagination is done by an array of objects, due angularJS's funky way of monitoring state
    with simple values */

    reloadView = function () {
        // Reset data
        vm.listViewResultSet = {
            totalPages: 0,
            items: []
        };
        vm.pagination = [];
        memberExtResource.getMembers(vm.options).then(function (data) {

            vm.listViewResultSet = data;

            //for (var i = vm.listViewResultSet.totalPages - 1; i >= 0; i--) {
            //    vm.pagination[i] = { index: i, name: i + 1 };
            //}

            //if (vm.options.pageNumber > vm.listViewResultSet.totalPages) {
            //    vm.options.pageNumber = vm.listViewResultSet.totalPages;
            //}
            if (vm.options.pageNumber > vm.listViewResultSet.totalPages) {
                vm.options.pageNumber = vm.listViewResultSet.totalPages;
            }

            vm.pagination = [];

            //list 10 pages as per normal
            if (vm.listViewResultSet.totalPages <= 10) {
                for (let i = 0; i < vm.listViewResultSet.totalPages; i++) {
                    vm.pagination.push({
                        val: i + 1,
                        isActive: vm.options.pageNumber === i + 1
                    });
                }
            }
            else {
                //if there is more than 10 pages, we need to do some fancy bits

                //get the max index to start
                var maxIndex = vm.listViewResultSet.totalPages - 10;
                //set the start, but it can't be below zero
                var start = Math.max(vm.options.pageNumber - 5, 0);
                //ensure that it's not too far either
                start = Math.min(maxIndex, start);

                for (let i = start; i < 10 + start; i++) {
                    vm.pagination.push({
                        val: i + 1,
                        isActive: vm.options.pageNumber === i + 1
                    });
                }

                //now, if the start is greater than 0 then '1' will not be displayed, so do the elipses thing
                if (start > 0) {
                    vm.pagination.unshift({ name: "First", val: 1, isActive: false }, { val: "...", isActive: false });
                }

                //same for the end
                if (start < maxIndex) {
                    vm.pagination.push({ val: "...", isActive: false }, { name: "Last", val: vm.listViewResultSet.totalPages, isActive: false });
                }
            }

        });
        //angularHelper.getCurrentForm($scope).$setPristine();
        buildFilterButtons();
    };

    //assign debounce method to the search to limit the queries
    vm.search = _.debounce(function () {
        $scope.$apply(function () {
            vm.options.pageNumber = 1;
            reloadView();
        });
    }, 500);

    vm.selectAll = function ($event) {
        var checkbox = $event.target;
        if (!angular.isArray(vm.listViewResultSet.items)) {
            return;
        }
        for (var i = 0; i < vm.listViewResultSet.items.length; i++) {
            var entity = vm.listViewResultSet.items[i];
            entity.selected = checkbox.checked;
        }
    };

    vm.isSelectedAll = function () {
        if (!angular.isArray(vm.listViewResultSet.items)) {
            return false;
        }
        return _.every(vm.listViewResultSet.items, function (item) {
            return item.selected;
        });
    };

    vm.isAnythingSelected = function () {
        if (!angular.isArray(vm.listViewResultSet.items)) {
            return false;
        }
        return _.some(vm.listViewResultSet.items, function (item) {
            return item.selected;
        });
    };

    vm.canUnlock = function () {
        if (!angular.isArray(vm.listViewResultSet.items)) {
            return false;
        }
        return _.some(vm.listViewResultSet.items, function (item) {
            return item.selected && item.isLockedOut;
        });

    };

    vm.canApprove = function () {
        if (!angular.isArray(vm.listViewResultSet.items)) {
            return false;
        }
        return _.some(vm.listViewResultSet.items, function (item) {
            return item.selected && !item.isApproved;
        });

    };

    vm.canSuspend = function () {
        if (!angular.isArray(vm.listViewResultSet.items)) {
            return false;
        }
        return _.some(vm.listViewResultSet.items, function (item) {
            return item.selected && item.isApproved;
        });

    };
    vm.getIcon = function (entry) {
        return iconHelper.convertFromLegacyIcon(entry.icon);
    };

    vm.getLockedIcon = function (entry) {
        return entry.isLockedOut ? 'icon-lock color-red' : 'icon-unlocked color-green';
    };

    vm.getSuspendedIcon = function (entry) {
        return entry.isApproved ? 'icon-check color-green' : 'icon-block color-red';
    };

    vm.getLockedDescription = function (entry) {
        return entry.isLockedOut ? 'Locked Out' : 'Unlocked';
    };

    vm.getSuspendedDescription = function (entry) {
        return entry.isApproved ? 'Approved' : 'Suspended';
    };
    vm.delete = function () {
        var selected = _.filter(vm.listViewResultSet.items, function (item) {
            return item.selected;
        });
        var total = selected.length;
        if (total === 0) {
            return;
        }

        if (confirm("Sure you want to delete?") === true) {
            vm.actionInProgress = true;
            vm.bulkStatus = "Starting with delete";
            var current = 1;

            for (var i = 0; i < selected.length; i++) {
                memberResource.deleteByKey(selected[i].key).then(function (data) {
                    vm.bulkStatus = "Deleted member " + current + " out of " + total + " members";
                    if (current === total) {
                        notificationsService.success("Bulk action", "Deleted " + total + " members");
                        vm.bulkStatus = "";
                        $timeout(reloadView, 1000);
                        vm.actionInProgress = false;
                    }
                    current++;
                });
            }
        }

    };

    vm.exportFiltered = function () {
        memberExtResource.getMemberExport(vm.options);
    };

    vm.unlock = function () {
        var selected = _.filter(vm.listViewResultSet.items, function (item) {
            return item.selected && item.isLockedOut;
        });
        var total = selected.length;
        if (total === 0) {
            return;
        }

        vm.actionInProgress = true;
        vm.bulkStatus = "Starting with member unlock";
        var current = 1;

        for (var i = 0; i < selected.length; i++) {

            memberExtResource.unlockById(selected[i].key)
                .then(function (member) {
                    vm.bulkStatus = "Unlocking " + current + " out of " + total + " members";
                    if (current === total) {
                        notificationsService.success("Bulk action", "Unlocked " + total + " members");
                        vm.bulkStatus = "";
                        $timeout(reloadView, 1000);
                        vm.actionInProgress = false;
                    }
                    current++;
                }, function (err) {

                    vm.bulkStatus = "";
                    $timeout(reloadView, 1000);
                    vm.actionInProgress = false;

                    //if there are validation errors for publishing then we need to show them
                    if (err.status === 400 && err.data && err.data.Message) {
                        notificationsService.error("Member approval error", err.data.Message);
                    }
                    else {
                        dialogService.ysodDialog(err);
                    }
                });

        }
    };

    vm.approve = function () {
        var selected = _.filter(vm.listViewResultSet.items, function (item) {
            return item.selected && !item.isApproved;
        });
        var total = selected.length;
        if (total === 0) {
            return;
        }

        vm.actionInProgress = true;
        vm.bulkStatus = "Starting with member approval";
        var current = 1;

        for (var i = 0; i < selected.length; i++) {

            memberExtResource.approveById(selected[i].key)
                .then(function (member) {
                    vm.bulkStatus = "Approving " + current + " out of " + total + " members";
                    if (current === total) {
                        notificationsService.success("Bulk action", "Approved " + total + " members");
                        vm.bulkStatus = "";
                        $timeout(reloadView, 1000);
                        vm.actionInProgress = false;
                    }
                    current++;
                }, function (err) {

                    vm.bulkStatus = "";
                    $timeout(reloadView, 1000);
                    vm.actionInProgress = false;

                    //if there are validation errors for publishing then we need to show them
                    if (err.status === 400 && err.data && err.data.Message) {
                        notificationsService.error("Member approval error", err.data.Message);
                    }
                    else {
                        dialogService.ysodDialog(err);
                    }
                });

        }
    };

    vm.suspend = function () {
        var selected = _.filter(vm.listViewResultSet.items, function (item) {
            return item.selected && item.isApproved;
        });
        var total = selected.length;
        if (total === 0) {
            return;
        }

        vm.actionInProgress = true;
        vm.bulkStatus = "Starting with member suspension";
        var current = 1;

        for (var i = 0; i < selected.length; i++) {

            memberExtResource.suspendById(selected[i].key)
                .then(function (content) {
                    vm.bulkStatus = "Suspending " + current + " out of " + total + " members";

                    if (current === total) {
                        notificationsService.success("Bulk action", "Suspended " + total + " members");
                        vm.bulkStatus = "";
                        $timeout(reloadView, 1000);
                        vm.actionInProgress = false;
                    }

                    current++;
                });
        }
    };

    vm.editMember = function (key) {
        dialogService.closeAll();
        dialogService.open({
            template: '/app_plugins/MemberManager/backoffice/dialogs/member/edit.html',
            key: key,
            closeOnSave: true,
            //tabFilter: ["Generic properties"],
            callback: function (data) {
                reloadView();
            }
        });
    };

    vm.clearFilter = function () {
        dialogService.closeAll();
        vm.options.filter = {};
        vm.searchDisplay = null;
        reloadView();
    };

    vm.filterMembers = function () {
        dialogService.closeAll();
        dialogService.open({
            template: '/app_plugins/MemberManager/backoffice/dialogs/member/filter.html',
            closeOnSave: false,
            filter: vm.options.filter,
            memberTypes: vm.listViewAllowedTypes,
            callback: function (data) {
                vm.options.filter = data;
                vm.searchDisplay = data.display;
                reloadView();
            }
        });
    };

    vm.entityType = "member";
    vm.pagination = new Array(10);

    memberTypeResource.getTypes().then(function (data) {
        vm.listViewAllowedTypes = data;
    });
    reloadView();
}

angular.module("umbraco").controller("MemberManager.Dashboard.MemberListViewController", memberListViewController);