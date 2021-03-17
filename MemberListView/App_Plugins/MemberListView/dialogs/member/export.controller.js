angular.module("umbraco").controller("MemberManager.Dialogs.Member.ExportController",
    function ($scope, memberExtResource) {
        "use strict";

        $scope.vm = {
            filterData: $scope.model.filterData,
            exportData: {
                format: $scope.model.format ?? "Excel",
                columns: $scope.model.columns
            },
            totalItems: $scope.model.totalItems,
            memberTypes: $scope.model.memberTypes,
            columnList: []
        }

        function init() {
            memberExtResource.getMemberColumns($scope.vm.filterData.memberType).then(function (data) {
                // We use this to preserve the original filter data.
                $scope.vm.columnList = setSelected(data, $scope.vm.exportData.columns)
            })

        }

        // Loop through a list of select options and set selected for values that appear in a filter list
        setSelected = function (list, filter) {
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

            return newList        }

        processColumnList = function (list) {
            var filteredList = _.filter(list, function (i) {
                return i.selected
            })

            return _.map(filteredList, function (item) {
                return item.alias
            })
        }

        // Methods to manage select/deselect all in checkbox lists.
        $scope.selectAll = function (selectionList) {
            const allSelected = $scope.allSelected(selectionList)
            _.each(selectionList, function (item) { item.selected = !allSelected })
        }

        $scope.allSelected = function (selectionList) {
            return _.filter(selectionList, function (item) { return item.selected }).length === selectionList.length
        }

        $scope.exportRecords = function () {
            $scope.vm.exportData.columns = processColumnList($scope.vm.columnList)

            $scope.model.submit($scope.vm.exportData)
        }

        init()
    })