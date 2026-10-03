// Incident card renderer (A3): the expanded content of an active-calls row —
// dispatch time, type-ID, address, the units strip and the comment timeline
// inline. Pure: render(card, labels) returns HTML, so the headless tests can
// pin the layout without a server.
var resgrid;
(function (resgrid) {
    var dispatch;
    (function (dispatch) {
        var callcard;
        (function (callcard) {
            function esc(value) {
                return String(value === null || value === undefined ? '' : value)
                    .replace(/&/g, '&amp;')
                    .replace(/</g, '&lt;')
                    .replace(/>/g, '&gt;')
                    .replace(/"/g, '&quot;');
            }
            // Custom-state "colors" arrive as a hex value (custom states) or a CSS
            // class like label-success (the defaults). Using one as the other is a
            // silent white-on-white failure — the same lesson as the Now Responding
            // panel.
            function chipClass(color) {
                return (!color || color.charAt(0) === '#') ? '' : ' ' + color;
            }
            function chipStyle(color) {
                return (color && color.charAt(0) === '#') ? 'background-color:' + esc(color) + ';' : '';
            }
            function render(card, labels) {
                if (!card) {
                    return '';
                }
                labels = labels || {};
                var l = function (key, fallback) { return labels[key] || fallback; };
                var html = '<div class="call-card-detail">';

                html += '<div class="call-card-head">';
                html += '<span class="call-card-time">' + esc(card.LoggedOn) + '</span>';
                if (card.Type) {
                    html += '<span class="call-card-typeid">' + esc(card.Type) + '</span>';
                }
                if (card.Number) {
                    html += '<strong class="call-card-number">' + esc(card.Number) + '</strong>';
                }
                html += '</div>';

                if (card.Address) {
                    html += '<div class="call-card-address">' + esc(card.Address) + '</div>';
                }

                if (card.Units && card.Units.length) {
                    html += '<div class="call-card-units"><span class="call-card-label">' + esc(l('units', 'Units')) + '</span>';
                    for (var i = 0; i < card.Units.length; i++) {
                        var unit = card.Units[i];
                        html += '<span class="call-card-chip' + chipClass(unit.StateColor) + '" style="' + chipStyle(unit.StateColor) + '">' +
                            esc(unit.Name) + ' — ' + esc(unit.State) + '</span>';
                    }
                    html += '</div>';
                }

                html += '<div class="call-card-notes"><span class="call-card-label">' + esc(l('notes', 'Comments')) + '</span>';
                if (card.Notes && card.Notes.length) {
                    html += '<ul class="call-card-timeline">';
                    for (var j = 0; j < card.Notes.length; j++) {
                        var note = card.Notes[j];
                        html += '<li><span class="call-card-note-name">' + esc(note.Name) + '</span>' +
                            '<span class="call-card-note-time">' + esc(note.Timestamp) + '</span>' +
                            '<div class="call-card-note-text">' + esc(note.Note) + '</div></li>';
                    }
                    html += '</ul>';
                } else {
                    html += '<p class="text-muted call-card-none">' + esc(l('noNotes', 'No comments yet.')) + '</p>';
                }
                html += '</div>';

                html += '</div>';
                return html;
            }
            callcard.render = render;
        })(callcard = dispatch.callcard || (dispatch.callcard = {}));
    })(dispatch = resgrid.dispatch || (resgrid.dispatch = {}));
})(resgrid || (resgrid = {}));
