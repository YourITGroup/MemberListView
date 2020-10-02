function actionController($scope, localizationService) {

    localizationService.localize($scope.model.actionKey).then(function (value) {
        $scope.question = value
    });
}

angular.module("umbraco").controller("MemberListView.Overlays.ActionController", actionController);