﻿function URLTokenDecode(token) {

    if (token.length === 0) return null;

    // The last character in the token is the number of padding characters.
    var numberOfPaddingCharacters = token.slice(-1);

    // The Base64 string is the token without the last character.
    token = token.slice(0, -1);

    // '-'s are '+'s and '_'s are '/'s.
    token = token.replace(/-/g, '+');
    token = token.replace(/_/g, '/');

    // Pad the Base64 string out with '='s
    for (var i = 0; i < numberOfPaddingCharacters; i++)
        token += "=";

    return token;
}

$('#next-control').click(function () {
    var idx = parseInt($('#result-id').val());

    if (idx < results.length) {
        ShowDocument(idx + 1);
    }
});

$('#prev-control').click(function () {
    var idx = parseInt($('#result-id').val());

    if (idx > 0) {
        ShowDocument(idx - 1);
    }
});

// Details
/*
function ShowDocument(id) {
    $.post('/home/getdocumentbyid',
        {
            id: id
        },
        function (data) {
            result = data.result;

            var pivotLinksHTML = "";

            $('#details-pivot-content').html("");
            $('#reference-viewer').html("");
            $('#tag-viewer').html("");
            $('#details-viewer').html("").css("display", "none");

            $('#result-id').val(id);

            var fileContainerHTML = GetFileHTML(result);
            var transcriptContainerHTML = htmlDecode(result.content.trim());
            var fileName = "File";

            $('#details-pivot-content').html(`<div id="file-pivot" class="ms-Pivot-content" data-content="file">
                                            <div id="file-viewer" style="height: 100%;"></div>
                                        </div>
                                        <div id="transcript-pivot" class="ms-Pivot-content" data-content="transcript">
                                            <div id="transcript-viewer" style="height: 100%;">
                                                <div id='transcript-div'>
                                                    <pre id="transcript-viewer-pre"></pre>
                                                </div>
                                            </div>
                                        </div>`);

            $('#file-viewer').html(fileContainerHTML);
            $('#transcript-viewer-pre').html(transcriptContainerHTML);

            pivotLinksHTML += `<li id="file-pivot-link" class="ms-Pivot-link is-selected" data-content="file" title="File" tabindex="1">${fileName}</li>
                       <li id="transcript-pivot-link" class="ms-Pivot-link " data-content="transcript" title="Transcript" tabindex="1">Transcript</li>`;

            var tagContainerHTML = GetTagsHTML(result);

            $('#details-pivot-links').html(pivotLinksHTML);
            $('#tag-viewer').html(tagContainerHTML);
            $('#details-modal').modal('show');

            var PivotElements = document.querySelectorAll(".ms-Pivot");
            for (var i = 0; i < PivotElements.length; i++) {
                new fabric['Pivot'](PivotElements[i]);
            }

            //Log Click Events
            LogClickAnalytics(result.metadata_storage_name, 0);
        });
}*/


function ShowDocument(index) {
    result = results[index].document;

    var pivotLinksHTML = "";

    $('#details-pivot-content').html("");
    $('#reference-viewer').html("");
    $('#tag-viewer').html("");
    $('#details-viewer').html("").css("display", "none");

    $('#result-id').val(index);

    var path = atob(URLTokenDecode(result.metadata_storage_path)) + token;

    var fileContainerHTML = GetFileHTML(path);
    var fileName = "File";

    /*
    if (result.fileName !== "") {
        fileName = result.fileName;
    }
    */

    $('#details-pivot-content').html(`<div id="file-pivot" class="ms-Pivot-content" data-content="file">
                                        <div id="file-viewer" style="height: 100%;"></div>
                                    </div>
                                    <div id="transcript-pivot" class="ms-Pivot-content" data-content="transcript">
                                        <div id="transcript-viewer" style="height: 100%;">
                                            <div id='transcript-div'>
                                                <pre id="transcript-viewer-pre"></pre>
                                            </div>
                                        </div>
                                    </div>`);

    $('#file-viewer').html(fileContainerHTML);
    $('#transcript-viewer-pre').text(result.content.trim());

    pivotLinksHTML += `<li id="file-pivot-link" class="ms-Pivot-link is-selected" data-content="file" title="File Preview" tabindex="1">File</li>
                        <li id="transcript-pivot-link" class="ms-Pivot-link " data-content="transcript" title="Transcript" tabindex="1">Transcript</li>`;

    var tagContainerHTML = GetTagsHTML(result);

    $('#details-pivot-links').html(pivotLinksHTML);
    $('#tag-viewer').html(tagContainerHTML);
    $('#details-modal').modal('show');

    var PivotElements = document.querySelectorAll(".ms-Pivot");
    for (var i = 0; i < PivotElements.length; i++) {
        new fabric['Pivot'](PivotElements[i]);
    }

    GetSearchReferences(q);
}

function GetMatches(string, regex, index) {
    var matches = [];
    var match;
    while (match === regex.exec(string)) {
        matches.push(match[index]);
    }
    return matches;
}

function GetFileHTML(path) {

    if (path !== null) {

        var pathLower = path.toLowerCase();
        var fileContainherHTML;

        if (pathLower.includes(".pdf")) {
            fileContainerHTML =
                `<object class="file-container" data="${path}" type="application/pdf">
                    <iframe class="file-container" src="${path}" type="application/pdf">
                        This browser does not support PDFs. Please download the XML to view it: <a href="${path}">Download PDF</a>"
                    </iframe>
                </object>`;
        }
        else if (pathLower.includes(".jpg") || pathLower.includes(".jpeg") || pathLower.includes(".gif") || pathLower.includes(".png")) {
            fileContainerHTML =
                `<div class="file-container">
                    <img style='max-width:100%;' src="${path}"/>
                </div>`;
        }
        else if (pathLower.includes(".xml")) {
            fileContainerHTML =
                `<iframe class="file-container" src="${path}" type="text/xml">
                    This browser does not support XMLs. Please download the XML to view it: <a href="${path}">Download XML</a>"
                </iframe>`;
        }
        else if (pathLower.includes(".htm")) {
            var srcPrefixArr = result.metadata_storage_path.split('/');
            srcPrefixArr.splice(-1, 1);
            var srcPrefix = srcPrefixArr.join('/');

            var htmlContent = result.content.replace(/src=\"/gi, `src="${srcPrefix}/`);

            fileContainerHTML =
                `<iframe class="file-container" src="${path}"></iframe>`;
                //`${htmlContent}`;
        }
        else if (pathLower.includes(".mp3")) {
            fileContainerHTML =
                `<audio controls>
                  <source src="${path}" type="audio/mp3">
                  Your browser does not support the audio tag.
                </audio>`;
        }
        else if (pathLower.includes(".mp4")) {
            fileContainerHTML =
                `<video controls class="video-result">
                        <source src="${path}" type="video/mp4">
                        Your browser does not support the video tag.
                    </video>`;
        }
        else if (pathLower.includes(".doc") || pathLower.includes(".ppt") || pathLower.includes(".xls")) {
            var src = "https://view.officeapps.live.com/op/view.aspx?src=" + encodeURIComponent(path);

            fileContainerHTML =
                `<iframe class="file-container" src="${src}"></iframe>`;
        }
        else if (pathLower.includes(".txt")) {
            fileContainerHTML = `<iframe class="file-container" src="${path}"></iframe>`;
        }
        else {
            fileContainerHTML =
                `<div>This file cannot be previewed 1. Download it here to view: <a href="${path}">Download</a></div>`;

        } 
    }
    else {
        fileContainerHTML =
            `<div>This file cannot be previewed or downloaded.`;
    }

    return fileContainerHTML;
}

function GetSearchReferences(q) {
    var copy = q;

    copy = copy.replace(/~\d+/gi, "");
    matches = GetMatches(copy, /\w+/gi, 0);

    matches.forEach(function (match) {
        GetReferences(match, true);
    });
}

function SearchTranscript(searchText) {
    $('#reference-viewer').html("");

    if (searchText !== "") {
        // get whole phrase
        GetReferences(searchText, false);
    }
}