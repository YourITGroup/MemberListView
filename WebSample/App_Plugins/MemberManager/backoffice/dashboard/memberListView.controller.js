function memberListViewController($rootScope, $scope, $routeParams, $injector, $timeout, notificationsService, iconHelper, dialogService, umbRequestHelper) {

    var memberResource, memberTypeResource;
    memberResource = $injector.get('memberResource');
    memberExtResource = $injector.get('memberExtResource');
    memberTypeResource = $injector.get('memberTypeResource');

    $scope.pagination = [];
    $scope.isNew = false;
    $scope.actionInProgress = false;
    $scope.listViewResultSet = {
        totalPages: 0,
        items: []
    };

    $scope.options = {
        pageSize: 10,
        pageNumber: ($routeParams.page && Number($routeParams.page) != NaN && Number($routeParams.page) > 0) ? $routeParams.page : 1,
        filter: {},
        orderBy: 'Name',
        orderDirection: "asc",
        //orderBy: ($scope.model.config.orderBy ? $scope.model.config.orderBy : 'Name').trim(),
        //orderDirection: $scope.model.config.orderDirection ? $scope.model.config.orderDirection.trim() : "asc"
    };

    $scope.isSortDirection = function (col, direction) {
        return $scope.options.orderBy.toUpperCase() == col.toUpperCase() && $scope.options.orderDirection == direction;
    }

    $scope.next = function () {
        if ($scope.options.pageNumber < $scope.listViewResultSet.totalPages) {
            $scope.options.pageNumber++;
            $scope.reloadView();
        }
    };

    $scope.goToPage = function (pageNumber) {
        $scope.options.pageNumber = pageNumber + 1;
        $scope.reloadView();
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

    $scope.prev = function () {
        if ($scope.options.pageNumber > 1) {
            $scope.options.pageNumber--;
            $scope.reloadView();
        }
    };

    /*Loads the search results, based on parameters set in prev,next,sort and so on*/
    /*Pagination is done by an array of objects, due angularJS's funky way of monitoring state
    with simple values */

    $scope.reloadView = function () {
        // Reset data
        $scope.listViewResultSet = {
            totalPages: 0,
            items: []
        };
        $scope.pagination = [];
        memberExtResource.getMembers($scope.options).then(function (data) {

            $scope.listViewResultSet = data;

            //for (var i = $scope.listViewResultSet.totalPages - 1; i >= 0; i--) {
            //    $scope.pagination[i] = { index: i, name: i + 1 };
            //}

            //if ($scope.options.pageNumber > $scope.listViewResultSet.totalPages) {
            //    $scope.options.pageNumber = $scope.listViewResultSet.totalPages;
            //}
            if ($scope.options.pageNumber > $scope.listViewResultSet.totalPages) {
                $scope.options.pageNumber = $scope.listViewResultSet.totalPages;
            }

            $scope.pagination = [];

            //list 10 pages as per normal
            if ($scope.listViewResultSet.totalPages <= 10) {
                for (var i = 0; i < $scope.listViewResultSet.totalPages; i++) {
                    $scope.pagination.push({
                        val: (i + 1),
                        isActive: $scope.options.pageNumber == (i + 1)
                    });
                }
            }
            else {
                //if there is more than 10 pages, we need to do some fancy bits

                //get the max index to start
                var maxIndex = $scope.listViewResultSet.totalPages - 10;
                //set the start, but it can't be below zero
                var start = Math.max($scope.options.pageNumber - 5, 0);
                //ensure that it's not too far either
                start = Math.min(maxIndex, start);

                for (var i = start; i < (10 + start) ; i++) {
                    $scope.pagination.push({
                        val: (i + 1),
                        isActive: $scope.options.pageNumber == (i + 1)
                    });
                }

                //now, if the start is greater than 0 then '1' will not be displayed, so do the elipses thing
                if (start > 0) {
                    $scope.pagination.unshift({ name: "First", val: 1, isActive: false }, { val: "...", isActive: false });
                }

                //same for the end
                if (start < maxIndex) {
                    $scope.pagination.push({ val: "...", isActive: false }, { name: "Last", val: $scope.listViewResultSet.totalPages, isActive: false });
                }
            }

        });
    };

    //assign debounce method to the search to limit the queries
    $scope.search = _.debounce(function () {
        $scope.options.pageNumber = 1;
        $scope.reloadView();
    }, 100);

    $scope.selectAll = function ($event) {
        var checkbox = $event.target;
        if (!angular.isArray($scope.listViewResultSet.items)) {
            return;
        }
        for (var i = 0; i < $scope.listViewResultSet.items.length; i++) {
            var entity = $scope.listViewResultSet.items[i];
            entity.selected = checkbox.checked;
        }
    };

    $scope.isSelectedAll = function () {
        if (!angular.isArray($scope.listViewResultSet.items)) {
            return false;
        }
        return _.every($scope.listViewResultSet.items, function (item) {
            return item.selected;
        });
    };

    $scope.isAnythingSelected = function () {
        if (!angular.isArray($scope.listViewResultSet.items)) {
            return false;
        }
        return _.some($scope.listViewResultSet.items, function (item) {
            return item.selected;
        });
    };

    $scope.canUnlock = function () {
        if (!angular.isArray($scope.listViewResultSet.items)) {
            return false;
        }
        return _.some($scope.listViewResultSet.items, function (item) {
            return item.selected && item.isLockedOut;
        });

    }

    $scope.canApprove = function () {
        if (!angular.isArray($scope.listViewResultSet.items)) {
            return false;
        }
        return _.some($scope.listViewResultSet.items, function (item) {
            return item.selected && !item.isApproved;
        });

    }

    $scope.canSuspend = function () {
        if (!angular.isArray($scope.listViewResultSet.items)) {
            return false;
        }
        return _.some($scope.listViewResultSet.items, function (item) {
            return item.selected && item.isApproved;
        });

    }
    $scope.getIcon = function (entry) {
        return iconHelper.convertFromLegacyIcon(entry.icon);
    };

    $scope.getLockedIcon = function (entry) {
        return entry.isLockedOut ? 'icon-lock color-red' : 'icon-unlocked color-green';
    }

    $scope.getSuspendedIcon = function (entry) {
        return entry.isApproved ? 'icon-check color-green' : 'icon-block color-red';
    }

    $scope.getLockedDescription = function (entry) {
        return entry.isLockedOut ? 'Locked Out' : 'Unlocked';
    }

    $scope.getSuspendedDescription = function (entry) {
        return entry.isApproved ? 'Approved' : 'Suspended';
    }
    $scope.delete = function () {
        var selected = _.filter($scope.listViewResultSet.items, function (item) {
            return item.selected;
        });
        var total = selected.length;
        if (total === 0) {
            return;
        }

        if (confirm("Sure you want to delete?") == true) {
            $scope.actionInProgress = true;
            $scope.bulkStatus = "Starting with delete";
            var current = 1;

            for (var i = 0; i < selected.length; i++) {
                memberResource.deleteByKey(selected[i].key).then(function (data) {
                    $scope.bulkStatus = "Deleted member " + current + " out of " + total + " members";
                    if (current === total) {
                        notificationsService.success("Bulk action", "Deleted " + total + " members");
                        $scope.bulkStatus = "";
                        $timeout($scope.reloadView, 1000);
                        $scope.actionInProgress = false;
                    }
                    current++;
                });
            }
        }

    };

    $scope.exportFiltered = function () {
        memberExtResource.getMemberExport($scope.options);
    };

    $scope.unlock = function () {
        var selected = _.filter($scope.listViewResultSet.items, function (item) {
            return item.selected && item.isLockedOut;
        });
        var total = selected.length;
        if (total === 0) {
            return;
        }

        $scope.actionInProgress = true;
        $scope.bulkStatus = "Starting with member unlock";
        var current = 1;

        for (var i = 0; i < selected.length; i++) {

            memberExtResource.unlockById(selected[i].key)
                .then(function (member) {
                    $scope.bulkStatus = "Unlocking " + current + " out of " + total + " members";
                    if (current == total) {
                        notificationsService.success("Bulk action", "Unlocked " + total + " members");
                        $scope.bulkStatus = "";
                        $timeout($scope.reloadView, 1000);
                        $scope.actionInProgress = false;
                    }
                    current++;
                }, function (err) {

                    $scope.bulkStatus = "";
                    $timeout($scope.reloadView, 1000);
                    $scope.actionInProgress = false;

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

    $scope.approve = function () {
        var selected = _.filter($scope.listViewResultSet.items, function (item) {
            return item.selected && !item.isApproved;
        });
        var total = selected.length;
        if (total === 0) {
            return;
        }

        $scope.actionInProgress = true;
        $scope.bulkStatus = "Starting with member approval";
        var current = 1;

        for (var i = 0; i < selected.length; i++) {

            memberExtResource.approveById(selected[i].key)
                .then(function (member) {
                    $scope.bulkStatus = "Approving " + current + " out of " + total + " members";
                    if (current == total) {
                        notificationsService.success("Bulk action", "Approved " + total + " members");
                        $scope.bulkStatus = "";
                        $timeout($scope.reloadView, 1000);
                        $scope.actionInProgress = false;
                    }
                    current++;
                }, function (err) {

                    $scope.bulkStatus = "";
                    $timeout($scope.reloadView, 1000);
                    $scope.actionInProgress = false;

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

    $scope.suspend = function () {
        var selected = _.filter($scope.listViewResultSet.items, function (item) {
            return item.selected && item.isApproved;
        });
        var total = selected.length;
        if (total === 0) {
            return;
        }

        $scope.actionInProgress = true;
        $scope.bulkStatus = "Starting with member suspension";
        var current = 1;

        for (var i = 0; i < selected.length; i++) {

            memberExtResource.suspendById(selected[i].key)
                .then(function (content) {
                    $scope.bulkStatus = "Suspension " + current + " out of " + total + " members";

                    if (current == total) {
                        notificationsService.success("Bulk action", "Suspended " + total + " members");
                        $scope.bulkStatus = "";
                        $timeout($scope.reloadView, 1000);
                        $scope.actionInProgress = false;
                    }

                    current++;
                });
        }
    };

    $scope.editMember = function (id) {
        dialogService.closeAll();
        dialogService.open({
            template: '/app_plugins/MemberManager/backoffice/dialogs/member/edit.html',
            id: id,
            closeOnSave: true,
            //tabFilter: ["Generic properties"],
            callback: function (data) {
                $scope.reloadView();
            }
        });
    };

    $scope.clearFilter = function () {
        dialogService.closeAll();
        $scope.options.filter = {};
        $scope.searchDisplay = null;
        $scope.reloadView();
    };

    $scope.filterMembers = function () {
        dialogService.closeAll();
        dialogService.open({
            template: '/app_plugins/MemberManager/backoffice/dialogs/member/filter.html',
            closeOnSave: false,
            filter: $scope.options.filter,
            memberTypes: $scope.listViewAllowedTypes,
            callback: function (data) {
                $scope.options.filter = data;
                $scope.searchDisplay = data.display;
                $scope.reloadView();
            }
        });
    };

    $scope.entityType = "member";
    $scope.pagination = new Array(10);
    memberTypeResource.getTypes().then(function (data) {
        $scope.listViewAllowedTypes = data;
    });
    $scope.reloadView();
}

angular.module("umbraco").controller("MemberManager.Dashboard.MemberListViewController", memberListViewController);