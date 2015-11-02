/// <reference path="../typings/jquery/jquery.d.ts" />
var HttpClient = (function () {
    function HttpClient() {
    }
    /**
    **/
    HttpClient.prototype.get = function (url, callback, errorCalback, statusCodeCallback) {
        $.get(url, callback);
    };
    /** something to say
     * @param {string} url very good
     * @return {void}
     */
    HttpClient.prototype.post = function (url, dataToSave, callback, errorCalback, statusCodeCallback) {
        this.executeAjax(url, dataToSave, "POST", callback, errorCalback, statusCodeCallback);
    };
    HttpClient.prototype.put = function (url, dataToSave, callback, errorCalback, statusCodeCallback) {
        this.executeAjax(url, dataToSave, "PUT", callback, errorCalback, statusCodeCallback);
    };
    HttpClient.prototype.delete = function (url, callback, errorCalback, statusCodeCallback) {
        this.executeAjax(url, null, "DELETE", callback, errorCalback, statusCodeCallback);
    };
    HttpClient.prototype.executeAjax = function (url, dataToSave, httpVerb, callback, errorCallback, statusCodeCallback) {
        //http://api.jquery.com/jquery.ajax/
        $.ajax(url, {
            data: dataToSave,
            type: httpVerb,
            success: function (data, textStatus, jqXHR) {
                if (callback !== null) {
                    callback(data);
                }
            },
            error: function (xhr, ajaxOptions, thrown) {
                if (errorCallback != null) {
                    errorCallback(xhr, ajaxOptions, thrown);
                }
            },
            statusCode: statusCodeCallback,
        });
    };
    return HttpClient;
})();
//# sourceMappingURL=HttpClient.js.map