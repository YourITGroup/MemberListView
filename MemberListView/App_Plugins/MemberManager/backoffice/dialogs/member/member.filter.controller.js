angular.module("umbraco").controller("MemberManager.Dialogs.Member.FilterController",
    function ($scope) {

        $scope.defaultButton = null;
        $scope.subButtons = [];
        var dialogOptions = $scope.$parent.dialogOptions;

        $scope.filterData = dialogOptions.filterData;
        $scope.memberTypes = dialogOptions.memberTypes;
        $scope.memberGroups = dialogOptions.memberGroups;

        function init() {
            var buttons = {
                defaultButton: createButtonDefinition("F"),
                subButtons: [
                    createButtonDefinition("C")
                ]
            };

            $scope.defaultButton = buttons.defaultButton;
            $scope.subButtons = buttons.subButtons;

            // We use this to preserve the original filter data.
            $scope.memberGroupFilter = setSelected($scope.memberGroups, $scope.filterData.memberGroups);
        }

        function createButtonDefinition(ch) {
            switch (ch) {
                case "F":
                    //publish action
                    return {
                        letter: ch,
                        labelKey: "memberManager_applyFilter",
                        label: "Apply Filter",
                        handler: $scope.applyFilter,
                        hotKey: "ctrl+f",
                        hotKeyWhenHidden: true,
                        alias: "applyFilter"
                    };
                case "C":
                    //send to publish
                    return {
                        letter: ch,
                        labelKey: "memberManager_clearFilter",
                        label: "Clear Filter",
                        handler: $scope.clearFilter,
                        hotKey: "ctrl+c",
                        hotKeyWhenHidden: true,
                        alias: "sendToPublish"
                    };
                default:
                    return null;
            }
        }

        // Generate model for representing the search
        getFilterModel = function () {
            var displaySearch = new Array();

            //if ($scope.filterData.filter) {
            //    displaySearch.push({ title: "Search", value: $scope.filterData.filter });
            //}
            if ($scope.filterData.memberType) {
                displaySearch.push({ title: "Member Type", value: _getMemberTypeName() });
            }
            if ($scope.filterData.f_umbracoMemberApproved) {
                displaySearch.push({ title: "Approved", value: $scope.filterData.f_umbracoMemberApproved === "true,1" ? "Approved" : "Suspended" });
            }

            if ($scope.filterData.f_umbracoMemberLockedOut) {
                displaySearch.push({ title: "Locked Out", value: $scope.filterData.f_umbracoMemberLockedOut === "true,1" ? "Locked Out" : "Active" });
            }

            return {
                filter: $scope.filterData.filter,
                memberType: $scope.filterData.memberType,
                memberGroups: processFilterList(displaySearch, "Member Groups", $scope.memberGroupFilter),
                f_umbracoMemberApproved: !$scope.filterData.f_umbracoMemberApproved ? "" : $scope.filterData.f_umbracoMemberApproved,
                f_umbracoMemberLockedOut: !$scope.filterData.f_umbracoMemberLockedOut ? "" : $scope.filterData.f_umbracoMemberLockedOut,

                display: displaySearch
            };
        };

        function _getMemberTypeName() {

            var type = _.filter($scope.memberTypes, function (item) {
                return item.alias === $scope.filterData.memberType;
            });

            return _.map(type, function (item) {
                return ' ' + item.name;
            }).join();

        }

        // Loop through a list of select options and set selected for values that appear in a filter list
        setSelected = function (list, filter) {
            // Convert string arrays to a select item object.
            list = _.map(list, function (item) {
                if (typeof item === "string") {
                    let value = item;
                    item = {
                        id: value.replace(" ", "_"),
                        name: value,
                        alias: value,
                        selected: false
                    };
                }
                return item;
            });

            if (filter) {
                for (var i = 0; i < filter.length; i++) {
                    for (var j = 0; j < list.length; j++) {
                        if (list[j].id === filter[i].replace(" ", "_")) {
                            list[j].selected = true;
                        }
                    }
                }
            }

            return list;
        };


        processFilterList = function (display, title, list) {
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
                return item.alias;
            });
        };

        $scope.applyFilter = function () {
            $scope.submit(getFilterModel());
        };

        $scope.clearFilter = function () {
            $scope.filterData = {
                filter: null
            };
            $scope.submit($scope.filterData);
        };

        // this method is called for all action buttons and then we proxy based on the btn definition
        $scope.performAction = function (btn) {

            if (!btn || !angular.isFunction(btn.handler)) {
                throw "btn.handler must be a function reference";
            }

            btn.handler.apply(this);
        };


        init();
    });