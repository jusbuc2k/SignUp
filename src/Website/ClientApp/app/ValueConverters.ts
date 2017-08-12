export class GradeDisplayValueConverter {
    toView(value) {
        value = parseInt(value, 10);
        if (value == null) {
            return "N/A"
        } else {
            switch (value) {
                case -1: return "Pre-K";
                case 0: return "Kindergarten";
                case 1: return "1st";
                case 2: return "2nd";
                case 3: return "3rd";
                case 4: return "4th";
                case 5: return "5th";
                case 6: return "6th";
                default: return "Other";
            }
        }
    }
}