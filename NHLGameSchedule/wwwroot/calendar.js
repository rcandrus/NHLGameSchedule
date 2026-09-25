window.nhlCalendar = {
    getLocalGameInfo: function (games) {
        return Object.fromEntries(games.map(function (game) {
            var date = new Date(game.utc);
            var dateKey = [
                date.getFullYear(),
                String(date.getMonth() + 1).padStart(2, "0"),
                String(date.getDate()).padStart(2, "0")
            ].join("-");

            return [game.id, {
                dateKey: dateKey,
                time: date.toLocaleTimeString([], {
                    hour: "numeric",
                    minute: "2-digit",
                    hour12: true
                }).replace(/\s?(AM|PM)$/i, function (_, meridiem) {
                    return meridiem.toLowerCase() === "am" ? "a" : "p";
                })
            }];
        }));
    }
};
