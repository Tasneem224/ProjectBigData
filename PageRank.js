function copyToClipboard(text) {
    navigator.clipboard.writeText(text).then(function () {
        alert('تم نسخ الرابط!');
    }, function (err) {
        alert('فشل النسخ: ' + err);
    });
}

function highlightText(keyword, element) {
    element.classList.toggle('clicked');

    var keywords = keyword.split(' ').filter(k => k.trim() !== '');
    var links = document.querySelectorAll('.link');

    links.forEach(link => {
        var linkText = link.textContent;
        link.innerHTML = '🔗 ' + linkText.substring(2);
    });

    links.forEach(link => {
        var linkText = link.innerHTML;
        keywords.forEach(k => {
            var escapedKeyword = k.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
            var regex = new RegExp(`\\b(${escapedKeyword})\\b`, 'gi');
            linkText = linkText.replace(regex, '<span class="highlight">$1</span>');
        });
        link.innerHTML = linkText;
    });
}
