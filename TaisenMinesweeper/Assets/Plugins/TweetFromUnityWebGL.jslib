mergeInto(LibraryManager.library, {
    TweetFromUnity: function (rawMessage, tags) {
        var message = Pointer_stringify(rawMessage);
        var mobilePattern = /android|iphone|ipad|ipod/i;

        var ua = window.navigator.userAgent.toLowerCase();

        tags_str = tags[0];
        for (var i = 1; i < tags.length; i++) {
            tags += "," + tags[i]
        }

        if (ua.search(mobilePattern) !== -1 || (ua.indexOf("macintosh") !== -1 && "ontouchend" in document)) {
            // Mobile
            location.href = "twitter://post?message=" + message + "&hashtags=taisen-minesweeper,minesweeper";
        } else {
            // PC
            window.open("https://twitter.com/intent/tweet?text=" + message + "&hashtags=TaisenMinesweeper,Minesweeper", "_blank");
        }
    },
});
