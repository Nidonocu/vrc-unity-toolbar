(async function main () {
    const response = await fetch('README.md');

    if (response.ok) {
        const readme = await response.text();
        var converter = new showdown.Converter(),
        html = converter.makeHtml(readme);
        document.querySelector('#readme-content').innerHTML = html;
    }
})();
