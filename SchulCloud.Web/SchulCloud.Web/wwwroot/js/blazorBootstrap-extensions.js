'use strict';

window.blazorBootstrapExtensions = {
    checkBox: {
        setIndeterminate: function (elementId, state) {
            let element = document.getElementById(elementId);
            if (element != null) {
                element.indeterminate = state;
            }
        }
    }
}