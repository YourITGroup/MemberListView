function MemberFilterDialogController($scope, $routeParams, $q, $timeout, $window, appState, memberExtResource, entityResource, navigationService, notificationsService, angularHelper, serverValidationManager, contentEditingHelper, fileManager, formHelper, umbRequestHelper, umbModelMapper, $http) {
    //setup scope vars


    var dialogOptions = $scope.$parent.dialogOptions;

    $scope.filter = dialogOptions.filter;
    $scope.memberTypes = dialogOptions.memberTypes;


    //memberExtResource.getLocations().then(function (data) {
    //    setSelected(data, $scope.filter.f_location);
    //    $scope.filter.f_location = data;
    //});

    $scope.filterSearch = function () {
        $scope.submit(getFilterModel());
    }

    // Loop through a list of select options and set selected for values that appear in a filter list
    setSelected = function (list, filter) {
        if (filter) {
            for (var i = 0; i < filter.length; i++) {
                for (var j = 0; j < list.length; j++) {
                    if (list[j].id == filter[i]) {
                        list[j].selected = true;
                    }
                }
            }
        }
    }

    // Generate model for representing the search
    getFilterModel = function () {
        var displaySearch = new Array();

        if ($scope.filter.filter) {
            displaySearch.push({ title: "Search", value: $scope.filter.filter });
        }
        if ($scope.filter.memberType) {
            displaySearch.push({ title: "Member Type", value: _getMemberTypeName() });
        }
        if ($scope.filter.f_umbracoMemberApproved) {
            displaySearch.push({ title: "Approved", value: $scope.filter.f_umbracoMemberApproved == "true,1" ? "Approved" : "Suspended" });
        }
        if ($scope.filter.f_umbracoMemberLockedOut) {
            displaySearch.push({ title: "Locked Out", value: $scope.filter.f_umbracoMemberLockedOut == "true,1" ? "Locked Out" : "Active" });
        }

        return {
            filter: $scope.filter.filter,
            memberType: $scope.filter.memberType,
            display: displaySearch,
            f_umbracoMemberApproved: !$scope.filter.f_umbracoMemberApproved ? "" : $scope.filter.f_umbracoMemberApproved,
            f_umbracoMemberLockedOut: !$scope.filter.f_umbracoMemberLockedOut ? "" : $scope.filter.f_umbracoMemberLockedOut,
            //f_location: _processFilterList(displaySearch, "Locations", $scope.filter.f_location)
        };
    }

    function _getMemberTypeName() {

        var type = _.filter($scope.memberTypes, function (item) {
            return (item.alias == $scope.filter.memberType);
        });

        return _.map(type, function (item) {
            return ' ' + item.name;
        }).join();

    }

    function _processFilterList(display, title, list) {
        var filteredList = _.filter(list, function (i) {
            return i.selected;
        });

        var displayVal = _.map(filteredList, function (item) {
            return ' ' + item.name;
        }).join();

        if (displayVal) {
            display.push({ title: title, value: displayVal });
        }
        return _.map(filteredList, function (item) {
            return item.id;
        });
    }
}

angular.module("umbraco").controller("MemberManager.Dialogs.Member.FilterController", MemberFilterDialogController);