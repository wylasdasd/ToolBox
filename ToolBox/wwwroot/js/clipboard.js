window.clipboardCopy = {
    copyText: function(text) {
        navigator.clipboard.writeText(text).then(function () {
            // Optional: Show a toast or something
            console.log("Copied to clipboard");
        })
        .catch(function (error) {
            console.error("Failed to copy:", error);
            alert("Failed to copy to clipboard: " + error);
        });
    },
    copyElementText: function(elementId) {
        var element = document.getElementById(elementId);
        if(element) {
            // Use innerText to get the visible text, preserving newlines
            // But innerText might include unexpected spacing depending on CSS.
            // For a <pre> tag, innerText usually works well.
            // Alternatively, we could pass the raw string from C# if we want exact control.
            // However, the user specifically asked to copy "content inside the pre tag".
            // So getting the text content of the element seems right.
            var text = element.innerText;
            this.copyText(text);
        } else {
            console.error("Element not found: " + elementId);
        }
    }
};
