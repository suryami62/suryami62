/**
 * Inserts text at the current cursor position or wraps selection.
 * @param {HTMLTextAreaElement} textarea
 * @param {string} prefix
 * @param {string} suffix
 */
export function insertText(textarea, prefix, suffix = "") {
    if (!textarea) return;

    const start = textarea.selectionStart;
    const end = textarea.selectionEnd;
    const text = textarea.value;
    const selection = text.substring(start, end);

    const before = text.substring(0, start);
    const after = text.substring(end);

    const newText = before + prefix + selection + suffix + after;
    textarea.value = newText;

    // Set cursor position back
    textarea.focus();
    const newCursorPos = start + prefix.length + selection.length + suffix.length;
    textarea.setSelectionRange(newCursorPos, newCursorPos);

    return newText;
}

/**
 * Gets the current selection from the textarea.
 */
export function getSelection(textarea) {
    if (!textarea) return "";
    return textarea.value.substring(textarea.selectionStart, textarea.selectionEnd);
}
