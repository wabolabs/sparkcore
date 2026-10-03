// Station strips (A5/A6): apparatus in/out of service, active weather alerts
// and upcoming events. Pure render functions so the headless tests can pin
// each strip without a server; the dashboard fills the containers from
// Dispatch/GetStationStrips.
var resgrid;
(function (resgrid) {
    var dispatch;
    (function (dispatch) {
        var strips;
        (function (strips) {
            function esc(value) {
                return String(value === null || value === undefined ? '' : value)
                    .replace(/&/g, '&amp;')
                    .replace(/</g, '&lt;')
                    .replace(/>/g, '&gt;')
                    .replace(/"/g, '&quot;');
            }
            function renderApparatus(apparatus, labels) {
                if (!apparatus) {
                    return '';
                }
                labels = labels || {};
                var html = '<div class="strip-line">';
                html += '<span class="strip-big">' + esc(apparatus.InService) + '</span> <span class="strip-label">' + esc(labels.inService || 'in service') + '</span>';
                html += '<span class="strip-big strip-oos">' + esc(apparatus.OutOfService) + '</span> <span class="strip-label">' + esc(labels.outOfService || 'out of service') + '</span>';
                html += '</div>';
                if (apparatus.OutOfServiceUnits && apparatus.OutOfServiceUnits.length) {
                    html += '<div class="strip-sub">' + esc(apparatus.OutOfServiceUnits.join(', ')) + '</div>';
                }
                return html;
            }
            function renderWeather(weather, labels) {
                labels = labels || {};
                if (!weather || !weather.Alerts || !weather.Alerts.length) {
                    return '<p class="text-muted strip-none">' + esc(labels.noAlerts || 'No active alerts') + '</p>';
                }
                var html = '';
                for (var i = 0; i < weather.Alerts.length; i++) {
                    var alert = weather.Alerts[i];
                    html += '<div class="strip-line">';
                    html += '<span class="strip-sev">' + esc(alert.Severity) + '</span> ';
                    html += '<strong>' + esc(alert.Event) + '</strong>';
                    html += '</div>';
                    if (alert.Headline) {
                        html += '<div class="strip-sub">' + esc(alert.Headline) + '</div>';
                    }
                }
                return html;
            }
            function renderEvents(events, labels) {
                labels = labels || {};
                if (!events || !events.Items || !events.Items.length) {
                    return '<p class="text-muted strip-none">' + esc(labels.noEvents || 'No upcoming events') + '</p>';
                }
                var html = '<ul class="strip-events">';
                for (var i = 0; i < events.Items.length; i++) {
                    var item = events.Items[i];
                    html += '<li><span class="strip-event-time">' + esc(item.Start) + '</span> ' +
                        '<span class="strip-event-title">' + esc(item.Title) + '</span></li>';
                }
                html += '</ul>';
                return html;
            }
            strips.renderApparatus = renderApparatus;
            strips.renderWeather = renderWeather;
            strips.renderEvents = renderEvents;
        })(strips = dispatch.strips || (dispatch.strips = {}));
    })(dispatch = resgrid.dispatch || (resgrid.dispatch = {}));
})(resgrid || (resgrid = {}));
