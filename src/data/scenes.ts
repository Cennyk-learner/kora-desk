// Photographs for the full-screen focus view. Files live in public/scenes (3840 px wide, 50 photographs).
// Credits and licences: docs/IMAGE_CREDITS.md
export interface Scene {
  file: string;
  place: string;
  sub: string;
  author: string;
  license: string;
  source: string;
}

export const SCENES: Scene[] = [
  { file: 'gullfoss-1.jpg', place: '古佛斯瀑布 · 冰岛', sub: 'Gullfoss，黄金圈上的雪后瀑布，2017 年 2 月', author: "Pierre-Selim Huard", license: 'CC BY 4.0', source: 'https://commons.wikimedia.org/wiki/File:Iceland_-_2017-02-22_-_Gullfoss_-_3677.jpg' },
  { file: 'gullfoss-2.jpg', place: '古佛斯瀑布 · 冰岛', sub: 'Gullfoss，冰川融水落入峡谷，冬季', author: "Pierre-Selim Huard", license: 'CC BY 4.0', source: 'https://commons.wikimedia.org/wiki/File:Iceland_-_2017-02-22_-_Gullfoss_-_3684.jpg' },
  { file: 'reynisdrangar.jpg', place: '雷尼斯德朗加海蚀柱 · 冰岛南岸', sub: 'Reynisdrangar，从 Reynisfjall 山俯瞰', author: "Jakub Fryš", license: 'CC BY-SA 4.0', source: 'https://commons.wikimedia.org/wiki/File:Reynisdrangar_basalt_rocks.jpg' },
  { file: 'reynisfjara.jpg', place: '雷尼斯法拉黑沙滩 · 冰岛', sub: 'Reynisfjara，远处是米达尔斯冰川 Mýrdalsjökull', author: "Jakub Hałun", license: 'CC BY-SA 4.0', source: 'https://commons.wikimedia.org/wiki/File:View_from_Reynisfjara_towards_M%C3%BDrdalsj%C3%B6kull%2C_Iceland%2C_20230501_1658_4068.jpg' },
  { file: 'storhofdi.jpg', place: '斯托尔霍夫迪 · 冰岛南部', sub: 'Storhöfði，海马岛（Heimaey）上的陡峭山崖', author: "Diego Delso", license: 'CC BY-SA 4.0', source: 'https://commons.wikimedia.org/wiki/File:Storhofthi%2C_Su%C3%B0urland%2C_Islandia%2C_2014-08-17%2C_DD_127.JPG' },
  { file: 'gotur.jpg', place: '戈图尔 · 冰岛米达尔斯赫雷普', sub: 'Götur，夏日绿色的山谷与云影', author: "Jakub Hałun", license: 'CC BY 4.0', source: 'https://commons.wikimedia.org/wiki/File:View_of_G%C3%B6tur_in_M%C3%BDrdalshreppur_municipality%2C_Iceland%2C_20240720_0818_2817.jpg' },
  { file: 'vatnsfjordur.jpg', place: '瓦特恩斯峡湾自然保护区 · 冰岛西峡湾', sub: 'Vatnsfjörður，西峡湾的荒野', author: "Diego Delso", license: 'CC BY-SA 4.0', source: 'https://commons.wikimedia.org/wiki/File:Reserva_natural_de_Vatnsfj%C3%B6r%C3%B0ur%2C_Vestfir%C3%B0ir%2C_Islandia%2C_2014-08-14%2C_DD_113.JPG' },
  { file: 'hverarond.jpg', place: '赫韦拉隆德 · 冰岛米湖附近', sub: 'Hverarönd，环岛 1 号公路穿过地热荒原', author: "Jakub Hałun", license: 'CC BY 4.0', source: 'https://commons.wikimedia.org/wiki/File:Route_1_near_Hverar%C3%B6nd%2C_Iceland%2C_20240716_1046_1224.jpg' },
  { file: 'lofoten-aurora.jpg', place: '洛福滕群岛的极光 · 挪威', sub: 'Flakstad，Lofoten，夜空中的绿色极光', author: "Johannes Groll", license: 'CC0', source: 'https://commons.wikimedia.org/wiki/File:Lofoten%2C_Norway_%28Unsplash%29.jpg' },
  { file: 'reine-toppoya.jpg', place: '雷讷附近的 Litl-Toppøya · 挪威洛福滕', sub: 'Reinefjorden 与 Festhæltinden 山，夏夜', author: "Ximonic (Simo Räsänen)", license: 'CC BY-SA 4.0', source: 'https://commons.wikimedia.org/wiki/File:Litl-Topp%C3%B8ya_by_Reinefjorden_and_Festh%C3%A6ltinden_in_Moskenes%2C_Nordland%2C_Norway%2C_2022_June.jpg' },
  { file: 'reine-village.jpg', place: '雷讷渔村 · 挪威洛福滕', sub: 'Reine，Moskenes，夏夜的红色渔屋', author: "Ximonic (Simo Räsänen)", license: 'CC BY-SA 4.0', source: 'https://commons.wikimedia.org/wiki/File:Houses_of_Reine_by_Gravdalsbukta%2C_Moskenes%2C_Nordland%2C_Norway%2C_2022_June.jpg' },
  { file: 'geirangerfjord.jpg', place: '盖朗厄尔峡湾 · 挪威', sub: 'Geirangerfjord，从 Ørnesvingen 观景台望去', author: "Ximonic (Simo Räsänen)", license: 'CC BY-SA 3.0', source: 'https://commons.wikimedia.org/wiki/File:Geirangerfjord_from_%C3%98rnesvingen%2C_2013_June.jpg' },
  { file: 'matterhorn-day.jpg', place: '马特洪峰 · 瑞士', sub: 'Matterhorn，2019 年 3 月', author: "Liridon", license: 'CC BY-SA 4.0', source: 'https://commons.wikimedia.org/wiki/File:Matterhorn%2C_March_2019_%2801%29.jpg' },
  { file: 'matterhorn-sunset.jpg', place: '日落时的马特洪峰 · 瑞士', sub: 'Matterhorn，2016 年', author: "Sam Ferrara", license: 'CC0', source: 'https://commons.wikimedia.org/wiki/File:Matterhorn_sunset_2016_%28Unsplash%29.jpg' },
  { file: 'eidi-faroe.jpg', place: '埃于附近的孤屋 · 法罗群岛', sub: 'Eiði，Eysturoy，海边孤零零的小屋', author: "Sanshiro KUBOTA from London, UK", license: 'CC BY 2.0', source: 'https://commons.wikimedia.org/wiki/File:Lonely_house_on_the_coast_near_Ei%C3%B0i_-_Faroe_Islands_%2850413406426%29.jpg' },
  { file: 'hvannhagi-faroe.jpg', place: '赫瓦纳瓦特恩湖 · 法罗群岛', sub: 'Hvannavatn，Suðuroy 岛上的悬崖与湖', author: "kallerna", license: 'CC BY-SA 4.0', source: 'https://commons.wikimedia.org/wiki/File:Hvannhagi_9.jpg' },
  { file: 'jokulsarlon-sunset.jpg', place: '杰古沙龙冰河湖 · 冰岛', sub: 'Jökulsárlón，日落时瓦特纳冰川下的浮冰', author: "Moyan Brenn from Italy", license: 'CC BY 2.0', source: 'https://commons.wikimedia.org/wiki/File:Iceland_-_Water_and_Ice.jpg' },
  { file: 'jokulsarlon-aurora.jpg', place: '杰古沙龙冰河湖的极光 · 冰岛', sub: 'Jökulsárlón，极光落在冰河湖上', author: "JavierOlivares", license: 'CC BY-SA 4.0', source: 'https://commons.wikimedia.org/wiki/File:J%C3%B6kuls%C3%A1rl%C3%B3n-Aurora.jpg' },
  { file: 'jokulsarlon-pink.jpg', place: '杰古沙龙冰河湖 · 冰岛', sub: 'Jökulsárlón，粉色的日出天空', author: "Eric Kilby", license: 'CC BY-SA 2.0', source: 'https://commons.wikimedia.org/wiki/File:Warm_Skies_Over_Glacier_Lagoon_-_Iceland.jpg' },
  { file: 'iceberg-blue.jpg', place: '蓝色冰山 · 杰古沙龙 · 冰岛', sub: 'Jökulsárlón，带拱形的蓝色浮冰', author: "Eric Kilby", license: 'CC BY-SA 2.0', source: 'https://commons.wikimedia.org/wiki/File:Different_Kind_of_Blue_Tooth_-_Iceland.jpg' },
  { file: 'skogafoss-1.jpg', place: '斯科加瀑布 · 冰岛南部', sub: 'Skógafoss，2017 年 8 月', author: "Jonashtand", license: 'CC BY-SA 4.0', source: 'https://commons.wikimedia.org/wiki/File:201708_Sk%C3%B3gafoss_05.jpg' },
  { file: 'skogafoss-2.jpg', place: '斯科加瀑布 · 冰岛', sub: 'Skógafoss，高约 60 米的瀑布', author: "Jakub Hałun", license: 'CC BY-SA 4.0', source: 'https://commons.wikimedia.org/wiki/File:Sk%C3%B3gafoss%2C_Iceland%2C_20230501_1447_3979.jpg' },
  { file: 'kirkjufell-1.jpg', place: '教会山 · 冰岛斯奈山半岛', sub: 'Kirkjufell，倒影中的山峰', author: "Ivars Krutainis", license: 'CC0', source: 'https://commons.wikimedia.org/wiki/File:Kirkjufell_mountain_%28Unsplash%29.jpg' },
  { file: 'kirkjufell-2.jpg', place: '教会山瀑布 · 冰岛', sub: 'Kirkjufell 与瀑布，Snæfellsnes 半岛', author: "Anjali Kiggal", license: 'CC BY-SA 4.0', source: 'https://commons.wikimedia.org/wiki/File:Kirkjufell_Mountain_in_Snaefellsness.jpg' },
  { file: 'vestrahorn-1.jpg', place: '维斯特拉角 · 冰岛斯托克内斯', sub: 'Vestrahorn，Stokksnes 的黎明', author: "Simaron from Lower Saxony, Germany", license: 'CC BY-SA 2.0', source: 'https://commons.wikimedia.org/wiki/File:Vestrahorn_Daybreak_-_Flickr_-_Simaron.jpg' },
  { file: 'vestrahorn-2.jpg', place: '维斯特拉角日落 · 冰岛', sub: 'Vestrahorn，落日下的倒影', author: "Gérard Cachon", license: 'CC BY 2.0', source: 'https://commons.wikimedia.org/wiki/File:Vestrahorn_at_sunset_%283-41_pm%29_-_Flickr_-_G%C3%A9rard_Cachon.jpg' },
  { file: 'vestrahorn-3.jpg', place: '维斯特拉角与黑沙滩 · 冰岛', sub: 'Vestrahorn，Stokksnes 海滩，2022 年 6 月', author: "Daniel Oberhaus", license: 'CC BY 4.0', source: 'https://commons.wikimedia.org/wiki/File:Vestrahorn_and_Stokksnes_beach_in_Iceland.jpg' },
  { file: 'landmannalaugar.jpg', place: '兰德曼纳劳卡 · 冰岛高地', sub: 'Landmannalaugar，从 Laugavegur 徒步道望去的彩色火山', author: "Borvan53", license: 'CC BY-SA 4.0', source: 'https://commons.wikimedia.org/wiki/File:View_on_Landmannalaugar_area_from_Laugavegur_2012_panoramic_2.jpg' },
  { file: 'vik-church-aurora.jpg', place: '维克教堂与极光 · 冰岛', sub: 'Víkurkirkja，清澈夜空下的极光（合成照片）', author: "AstroAnthony", license: 'CC BY 4.0', source: 'https://commons.wikimedia.org/wiki/File:Church_of_light.jpg' },
  { file: 'aurora-stafafell.jpg', place: '极光 · 冰岛斯塔法费尔', sub: 'Stafafell，北极光', author: "Marco Ottobelli", license: 'CC BY-SA 4.0', source: 'https://commons.wikimedia.org/wiki/File:Aurora_Borealis_from_the_rooftop_in_Iceland.jpg' },
  { file: 'svartifoss.jpg', place: '黑瀑布 · 冰岛瓦特纳冰川国家公园', sub: 'Svartifoss，玄武岩柱环绕的瀑布', author: "Giles Laurent", license: 'CC BY-SA 4.0', source: 'https://commons.wikimedia.org/wiki/File:038_Svartifoss_waterfall_%28Iceland%29_Photo_by_Giles_Laurent.jpg' },
  { file: 'oxararfoss.jpg', place: '欧克萨拉瀑布 · 冰岛辛格韦德利', sub: 'Öxarárfoss，Þingvellir 国家公园', author: "Diego Delso", license: 'CC BY-SA 4.0', source: 'https://commons.wikimedia.org/wiki/File:%C3%96xar%C3%A1rfoss%2C_Parque_Nacional_de_%C3%9Eingvellir%2C_Su%C3%B0urland%2C_Islandia%2C_2014-08-16%2C_DD_029.JPG' },
  { file: 'vatnajokull.jpg', place: '瓦特纳冰川 · 冰岛', sub: 'Vatnajökull，欧洲最大冰川的冰舌', author: "Holger Uwe Schmitt", license: 'CC BY-SA 4.0', source: 'https://commons.wikimedia.org/wiki/File:%2BDer_gr%C3%B6%C3%9Fte_Gletscher_Europas._05.jpg' },
  { file: 'ice-cave.jpg', place: '冰川蓝冰洞 · 冰岛', sub: 'Vatnajökull 国家公园', author: "Davide Cantelli", license: 'CC0', source: 'https://commons.wikimedia.org/wiki/File:Glacier-cave-man-iceland_%28Unsplash%29.jpg' },
  { file: 'dyrholaey.jpg', place: '迪尔霍拉伊拱门 · 冰岛维克', sub: 'Dyrhólaey', author: "Raul Taciu", license: 'CC0', source: 'https://commons.wikimedia.org/wiki/File:Dyrh%C3%B3laey_Arch_%28Unsplash%29.jpg' },
  { file: 'hvitserkur.jpg', place: '赫维察克海蚀柱 · 冰岛西北部', sub: 'Hvítserkur', author: "Jakub Hałun", license: 'CC BY 4.0', source: 'https://commons.wikimedia.org/wiki/File:Hv%C3%ADtserkur_Sea_Stack%2C_Northwestern_Region%2C_Iceland%2C_20240715_1125_0839.jpg' },
  { file: 'fjadrargljufur.jpg', place: '菲亚德拉峡谷 · 冰岛', sub: 'Fjaðrárgljúfur', author: "Wojciech Strzelecki \"Wojtrix\"", license: 'CC BY-SA 4.0', source: 'https://commons.wikimedia.org/wiki/File:Fja%C3%B0r%C3%A1rglj%C3%BAfur01%28ws%29.jpg' },
  { file: 'aoraki.jpg', place: '奥拉基 / 库克山 · 新西兰', sub: 'Aoraki / Mt Cook，从 Mueller Hut 步道上段远眺', author: "Michal Klajban", license: 'CC BY-SA 4.0', source: 'https://commons.wikimedia.org/wiki/File:Aoraki_-_Mt_Cook%2C_Aoraki_-_Mount_Cook_National_Park%2C_New_Zealand.jpg' },
  { file: 'angelus-hut.jpg', place: '天使小屋 · 新西兰尼尔森湖', sub: 'Angelus Hut，冬日的天使湖', author: "Michal Klajban", license: 'CC BY-SA 4.0', source: 'https://commons.wikimedia.org/wiki/File:Angelus_Hut_in_the_winter%2C_Nelson_Lakes_National_Park%2C_New_Zealand.jpg' },
  { file: 'tekapo.jpg', place: '特卡波湖 · 新西兰', sub: 'Lake Tekapo，岸边开着鲁冰花', author: "Krzysztof Golik", license: 'CC BY-SA 4.0', source: 'https://commons.wikimedia.org/wiki/File:Lake_Tekapo_01.jpg' },
  { file: 'ngauruhoe.jpg', place: '瑙鲁霍伊火山 · 新西兰', sub: 'Ngauruhoe 与 Ruapehu，从 Tongariro 山顶望去', author: "Eusebius", license: 'CC BY 3.0', source: 'https://commons.wikimedia.org/wiki/File:Le_Ngauruhoe_et_le_Ruapehu_vus_du_sommet_du_Tongariro.jpg' },
  { file: 'sylvester.jpg', place: '西尔维斯特湖日出 · 新西兰', sub: 'Lake Sylvester，Kahurangi 国家公园', author: "Michal Klajban", license: 'CC BY-SA 4.0', source: 'https://commons.wikimedia.org/wiki/File:Lake_Sylvester_during_the_sunrise%2C_Kahurangi%2C_New_Zealand.jpg' },
  { file: 'otira.jpg', place: '奥蒂拉河 · 新西兰亚瑟隘口', sub: 'Otira River，Arthur\'s Pass 国家公园', author: "Michal Klajban", license: 'CC BY-SA 4.0', source: 'https://commons.wikimedia.org/wiki/File:Otira_River_in_Arthur%27s_Pass_National_Park%2C_New_Zealand.jpg' },
  { file: 'sabine-pier.jpg', place: '萨宾小屋的栈桥 · 新西兰', sub: 'Lake Rotoroa，尼尔森湖国家公园', author: "Michal Klajban", license: 'CC BY-SA 4.0', source: 'https://commons.wikimedia.org/wiki/File:Pier_by_Sabine_Hut_16-9%2C_Nelson_Lakes_National_Park%2C_New_Zealand.jpg' },
  { file: 'tasman-lake.jpg', place: '塔斯曼湖 · 新西兰', sub: 'Tasman Lake，冰川湖全景', author: "Krzysztof Golik", license: 'CC BY-SA 4.0', source: 'https://commons.wikimedia.org/wiki/File:Tasman_Lake_panorama_%2803-05-06-07%29.jpg' },
  { file: 'fuji.jpg', place: '富士山日落 · 日本', sub: 'Mount Fuji，2025 年 3 月', author: "Romain Guy from San Francisco, USA", license: 'CC0', source: 'https://commons.wikimedia.org/wiki/File:Mount_Fuji_at_sunset%2C_March_2025.jpg' },
  { file: 'kiyomizu.jpg', place: '清水寺 · 日本京都', sub: 'Kiyomizu-dera，红叶季，世界文化遗产', author: "Martin Falbisoner", license: 'CC BY-SA 4.0', source: 'https://commons.wikimedia.org/wiki/File:Kiyomizu-dera%2C_Kyoto%2C_November_2016_-01.jpg' },
  { file: 'shirakawago.jpg', place: '白川乡的夜 · 日本', sub: 'Shirakawa-go，合掌造村落', author: "Jordy Meow", license: 'CC BY-SA 3.0', source: 'https://commons.wikimedia.org/wiki/File:Shirakawago.jpg' },
  { file: 'imagoura.jpg', place: '今子浦 · 日本兵库县', sub: 'Imagoura，山阴海岸', author: "663highland", license: 'CC BY 2.5', source: 'https://commons.wikimedia.org/wiki/File:Imagoura_Kasumi_Coast04bs4440.jpg' },
  { file: 'nz-glow.jpg', place: '雪山群峰 · 新西兰尼尔森湖国家公园', sub: 'Hukere Stream 与 Shift Creek 山谷之间的山峰', author: "Michal Klajban", license: 'CC BY-SA 4.0', source: 'https://commons.wikimedia.org/wiki/File:Peaks_between_Hukere_Stream_and_Shift_Creek_valley%2C_Nelson_Lakes_National_Park%2C_New_Zealand.jpg' }
];
