mergeInto(LibraryManager.library, {
  OpenWindow: function (urlPtr) {
    var url = UTF8ToString(urlPtr);
    window.open(url, '_blank');
  }
});