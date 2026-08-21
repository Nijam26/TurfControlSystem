// Thin wrapper around flatpickr for the Reports page.
// One input, "range" mode: a single click selects one day (single-date report),
// clicking a second day extends it into a date-range report.
window.turfReports = {
    picker: null,

    init: function (elementId, dotNetHelper, initialDateStr) {
        this.destroy();

        var toIsoDate = function (d) {
            var y = d.getFullYear();
            var m = String(d.getMonth() + 1).padStart(2, '0');
            var day = String(d.getDate()).padStart(2, '0');
            return y + '-' + m + '-' + day;
        };

        this.picker = flatpickr('#' + elementId, {
            mode: 'range',
            dateFormat: 'd M Y',
            defaultDate: [initialDateStr],
            onChange: function (selectedDates) {
                if (selectedDates.length === 0) return;

                var start = selectedDates[0];
                var end = selectedDates.length > 1 ? selectedDates[1] : selectedDates[0];

                dotNetHelper.invokeMethodAsync('OnDateRangeSelected', toIsoDate(start), toIsoDate(end));
            }
        });
    },

    destroy: function () {
        if (this.picker) {
            this.picker.destroy();
            this.picker = null;
        }
    }
};
