angular.module("umbraco").controller("MemberManager.Dialogs.Member.ExportController",
    function ($scope, memberExtResource) {

        $scope.defaultButton = null;
        $scope.subButtons = [];

        var dialogOptions = $scope.$parent.dialogOptions;

        $scope.filterData = dialogOptions.filterData;
        $scope.columns = dialogOptions.columns;
        //$scope.memberType = dialogOptions.filterData.memberType;
        $scope.totalItems = dialogOptions.totalItems;
        $scope.memberTypes = dialogOptions.memberTypes;
        $scope.format = "Excel";

        $scope.allColumnsSelected = false;

        function init() {
            var buttons = {
                defaultButton: createButtonDefinition("E"),
                subButtons: []
            };

            $scope.defaultButton = buttons.defaultButton;
            $scope.subButtons = buttons.subButtons;

            memberExtResource.getMemberColumns(dialogOptions.filterData.memberType).then(function (data) {
                // We use this to preserve the original filter data.
                $scope.columnList = setSelected(data, $scope.columns);
                $scope.allColumnsSelected = $scope.allSelected($scope.columnList);
            });

        }

        function createButtonDefinition(ch) {
            switch (ch) {
                case "E":
                    //export action
                    return {
                        letter: ch,
                        labelKey: "memberManager_export",
                        label: "Export",
                        handler: $scope.export,
                        hotKey: "ctrl+e",
                        hotKeyWhenHidden: true,
                        alias: "export"
                    };
                default:
                    return null;
            }
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

        processColumnList = function (list) {
            var filteredList = _.filter(list, function (i) {
                return i.selected;
            });

            return _.map(filteredList, function (item) {
                return item.alias;
            });
        };

        // Methods to manage select/deselect all in checkbox lists.
        $scope.selectAll = function (selectionList) {
            _.each(selectionList, function (item) { item.selected = $scope.allColumnsSelected; });
        };

        $scope.allSelected = function (selectionList) {
            return _.filter(selectionList, function (item) { return item.selected; }).length === selectionList.length;
        };

        $scope.export = function () {
            $scope.submit(processColumnList($scope.columnList));
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