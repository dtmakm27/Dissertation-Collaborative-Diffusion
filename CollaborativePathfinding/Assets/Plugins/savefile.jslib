var DownloadFilePlugin = {
    // Open file.
    // gameObjectNamePtr: Unique GameObject name. Required for calling back unity with SendMessage.
    // methodNamePtr: Callback method name on given GameObject.
    // filter: Filter files. Example filters:
    //     Match all image files: "image/*"
    //     Match all video files: "video/*"
    //     Match all audio files: "audio/*"
    //     Custom: ".plist, .xml, .yaml"
    // multiselect: Allows multiple file selection
    UploadFile: function(gameObjectNamePtr, methodNamePtr, filterPtr, multiselect) {
        gameObjectName = Pointer_stringify(gameObjectNamePtr);
        methodName = Pointer_stringify(methodNamePtr);
        filter = Pointer_stringify(filterPtr);

        // Delete if element exist
        var fileInput = document.getElementById(gameObjectName)
        if (fileInput) {
            document.body.removeChild(fileInput);
        }

        fileInput = document.createElement('input');
        fileInput.setAttribute('id', gameObjectName);
        fileInput.setAttribute('type', 'file');
        fileInput.setAttribute('style','display:none;');
        fileInput.setAttribute('style','visibility:hidden;');
        if (multiselect) {
            fileInput.setAttribute('multiple', '');
        }
        if (filter) {
            fileInput.setAttribute('accept', filter);
        }
        fileInput.onclick = function (event) {
            // File dialog opened
            this.value = null;
        };
        fileInput.onchange = function (event) {
            // multiselect works
            var urls = [];
            for (var i = 0; i < event.target.files.length; i++) {
                urls.push(URL.createObjectURL(event.target.files[i]));
            }
            // File selected
            SendMessage(gameObjectName, methodName, urls.join());

            // Remove after file selected
            document.body.removeChild(fileInput);
        }
        document.body.appendChild(fileInput);

        document.onmouseup = function() {
            fileInput.click();
            document.onmouseup = null;
        }
    },
DownloadFile: function(array, size, fileNamePtr)
    {
        var fileName = UTF8ToString(fileNamePtr);
    
        var bytes = new Uint8Array(size);
        for (var i = 0; i < size; i++)
        {
        bytes[i] = HEAPU8[array + i];
        }
    
        var blob = new Blob([bytes]);
        var link = document.createElement('a');
        link.href = window.URL.createObjectURL(blob);
        link.download = fileName;
    
        var event = document.createEvent("MouseEvents");
        event.initMouseEvent("click");
        link.dispatchEvent(event);
        window.URL.revokeObjectURL(link.href);
    }
};

mergeInto(LibraryManager.library, DownloadFilePlugin);
