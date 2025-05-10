function copyToClipboard(text) {
    navigator.clipboard.writeText(text).then(function () {
        alert('تم نسخ الرابط!');
    }, function (err) {
        alert('فشل النسخ: ' + err);
    });
}

function highlightText(keyword, element) {
    element.classList.toggle('clicked');
    const container = document.querySelector('.results-container');
    if (!container) return;

    const regex = new RegExp(`(${keyword})`, 'gi');

    // Iterate through each result link and highlight keyword
    const links = container.querySelectorAll('.link');
    links.forEach(link => {
        const text = link.textContent;
        const newHTML = text.replace(regex, '<span class="highlight">$1</span>');
        link.innerHTML = newHTML;
    });
}
