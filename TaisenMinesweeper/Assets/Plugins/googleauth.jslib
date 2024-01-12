var googleauth = {
    $funcs: {},

    Initialize_DLL: function(sendMsgPtr) {
        funcs.sendMsgPtr = sendMsgPtr;
    },
    
    Authorize_DLL: function () {

        function decodeJwt(token) {
            var base64Url = token.split('.')[1];
            var base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
            var jsonPayload = decodeURIComponent(atob(base64).split('').map(function(c) {
                return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2);
            }).join(''));

            return JSON.parse(jsonPayload);
        }

        
        window.handleCredentialResponse = (res) => {
            var msg = res.credential;
            console.log(res)
            var decodedIdToken = decodeJwt(msg)
            console.log(decodedIdToken)
            var encoder = new TextEncoder();
            var strBuffer = encoder.encode(decodedIdToken.sub + String.fromCharCode(0));
            var strPtr = _malloc(strBuffer.length);
            HEAP8.set(strBuffer, strPtr);
            Module["dynCall_vi"](funcs.sendMsgPtr, strPtr)
            _free(strPtr);
        }

        var s = document.createElement("script");
        s.setAttribute("src", "https://accounts.google.com/gsi/client")
        s.defer = true;
        s.async = true;
        document.head.appendChild(s);

        var d = document.createElement("div");
        d.setAttribute("id", "g_id_onload");
        d.setAttribute("data-client_id", "588094874211-qfmqv7ben17880s8oqdh2jtso3d0p720.apps.googleusercontent.com")
        d.setAttribute("data-callback", "handleCredentialResponse");
        document.body.appendChild(d);
    }
};

autoAddDeps(googleauth, "$funcs");
mergeInto(LibraryManager.library, googleauth);