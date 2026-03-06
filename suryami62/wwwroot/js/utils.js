window.utils = {
    copyTextToClipboard(text) {
        if (!navigator.clipboard) {
            // Fallback for browsers without clipboard API
            const textArea = document.createElement("textarea");
            textArea.value = text;
            textArea.style.position = "fixed";
            document.body.appendChild(textArea);
            textArea.focus();
            textArea.select();
            try {
                const successful = document.execCommand('copy');
                document.body.removeChild(textArea);
                return successful;
            } catch (err) {
                document.body.removeChild(textArea);
                return false;
            }
        }
        return navigator.clipboard.writeText(text).then(() => true).catch(() => false);
    }
};