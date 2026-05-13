namespace SmartBulbControllerWPF.Models;

public static class NbaTeams
{
    public static readonly IReadOnlyList<NbaTeam> All =
    [
        new(1,  "Atlanta Hawks",           200, 16,  46 ),
        new(2,  "Boston Celtics",          0,   122, 51 ),
        new(3,  "Brooklyn Nets",           0,   0,   0  ),
        new(4,  "Charlotte Hornets",       0,   120, 140),
        new(5,  "Chicago Bulls",           206, 17,  65 ),
        new(6,  "Cleveland Cavaliers",     134, 0,   56 ),
        new(7,  "Dallas Mavericks",        0,   83,  188),
        new(8,  "Denver Nuggets",          13,  34,  64 ),
        new(9,  "Detroit Pistons",         200, 16,  46 ),
        new(10, "Golden State Warriors",   29,  66,  138),
        new(11, "Houston Rockets",         206, 17,  65 ),
        new(12, "Indiana Pacers",          253, 187, 48 ),
        new(13, "LA Clippers",             200, 16,  46 ),
        new(14, "Los Angeles Lakers",      85,  37,  130),
        new(15, "Memphis Grizzlies",       93,  118, 169),
        new(16, "Miami Heat",              152, 0,   46 ),
        new(17, "Milwaukee Bucks",         0,   71,  27 ),
        new(18, "Minnesota Timberwolves",  12,  35,  64 ),
        new(19, "New Orleans Pelicans",    0,   22,  65 ),
        new(20, "New York Knicks",         0,   107, 182),
        new(21, "Oklahoma City Thunder",   0,   125, 195),
        new(22, "Orlando Magic",           0,   125, 197),
        new(23, "Philadelphia 76ers",      0,   107, 182),
        new(24, "Phoenix Suns",            229, 95,  32 ),
        new(25, "Portland Trail Blazers",  224, 58,  62 ),
        new(26, "Sacramento Kings",        91,  43,  130),
        new(27, "San Antonio Spurs",       196, 206, 211),
        new(28, "Toronto Raptors",         206, 17,  65 ),
        new(29, "Utah Jazz",               0,   43,  92 ),
        new(30, "Washington Wizards",      0,   43,  92 ),
    ];
}
