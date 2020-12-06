angular.module("umbraco").controller("MemberManager.Dialogs.Member.FilterController",
    function ($scope) {

        $scope.defaultButton = null
        $scope.subButtons = []

        $scope.vm = {
            filterData: $scope.model.filterData,
            // Use setSelected when you have a property that can have multiple selections in the filter.
            memberGroups: setSelected($scope.model.memberGroups, $scope.model.filterData.memberGroups),
            memberTypes: setSelected($scope.model.memberTypes, $scope.model.filterData.memberType)
        }

        if (!$scope.vm.filterData.memberType) {
            $scope.vm.filterData.memberType = ''
        }

        function init() {
            var buttons = {
                defaultButton: createButtonDefinition("F"),
                subButtons: [
                    createButtonDefinition("C")
                ]
            }

            $scope.defaultButton = buttons.defaultButton
            $scope.subButtons = buttons.subButtons
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
                    }
                case "C":
                    //send to publish
                    return {
                        letter: ch,
                        labelKey: "memberManager_clearFilter",
                        label: "Clear Filter",
                        handler: $scope.clearFilter,
                        hotKey: "ctrl+c",
                        hotKeyWhenHidden: true,
                        alias: "clearFilter"
                    }
                default:
                    return null
            }
        }

        // Generate model for representing the search
        function getFilterModel() {
            var displaySearch = []

            //if ($scope.filterData.filter) {
            //    displaySearch.push({ title: "Search", value: $scope.filterData.filter })
            //}
            if ($scope.vm.filterData.memberType && $scope.vm.filterData.memberType.length > 0) {
                displaySearch.push({ title: "Member Type", value: getMemberTypeName() })
            }
            if ($scope.vm.filterData.umbracoMemberApproved !== null) {
                displaySearch.push({ title: "Approved", value: $scope.vm.filterData.umbracoMemberApproved === 1 ? "Approved" : "Suspended" })
            }

            if ($scope.vm.filterData.umbracoMemberLockedOut !== null) {
                displaySearch.push({ title: "Locked Out", value: $scope.vm.filterData.umbracoMemberLockedOut === 1 ? "Locked Out" : "Active" })
            }

            return {
                filter: $scope.vm.filterData.filter,
                memberType: $scope.vm.filterData.memberType,
                memberGroups: processFilterList(displaySearch, "Member Groups", $scope.vm.memberGroups, true),
                umbracoMemberApproved: $scope.vm.filterData.umbracoMemberApproved,
                umbracoMemberLockedOut: $scope.vm.filterData.umbracoMemberLockedOut,

                // Additional filters should have the f_ prefix and should match the alias of the member property.

                display: displaySearch
            }
        }

        function getMemberTypeName() {

            var type = _.filter($scope.vm.memberTypes, function (item) {
                return item.alias === $scope.vm.filterData.memberType
            })

            return _.map(type, function (item) {
                return ' ' + item.name
            }).join()

        }

        // Loop through a list of select options and set selected for values that appear in a filter list
        function setSelected(list, filter) {
            // Convert string arrays to a select item object.
            const newList = _.map(list, function (item, index, list) {
                if (typeof list[index] === "string") {
                    let value = item
                    item = {
                        id: index,
                        name: value,
                        alias: value,
                        selected: false
                    }
                } else {
                    item = list[index]
                }
                return item
            })

            if (filter) {
                if (Array.isArray(filter)) {
                    for (var i = 0; i < filter.length; i++) {
                        for (var j = 0; j < newList.length; j++) {
                            if (newList[j].id === filter[i].replace(" ", "_")) {
                                newList[j].selected = true
                            }
                        }
                    }
                } else {
                    for (var i = 0; i < newList.length; i++) {
                        if (newList[i].id === filter.replace(" ", "_")) {
                            newList[i].selected = true
                        }
                    }
                }
            }

            return newList
        }


        function processFilterList (display, title, list, useId = false) {
            var filteredList = _.filter(list, function (i) {
                return i.selected
            })

            var displayVal = _.map(filteredList, function (item) {
                return ' ' + item.name
            }).join()

            if (displayVal) {
                display.push({ title: title, value: displayVal })
            }
            return _.map(filteredList, function (item) {
                return useId ? item.id : item.alias
            })
        }

        $scope.applyFilter = function () {
            $scope.model.submit(getFilterModel())
        }

        $scope.clearFilter = function () {
            $scope.model.submit({
                filter: null
            })
        }

        init()
    })