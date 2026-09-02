using System;

namespace Subtitle_draft_GMTPC.Services
{
    public static class JapaneseWordListRules
    {
        public const string DefaultRules = @"// --- Nhóm 1: Từ thông dụng (CV - Consonant Vowel) ---
(katana:ka/ta/na), (oyasumi:o/ya/su/mi), (kokoro:ko/ko/ro), (sakura:sa/ku/ra), (tsubasa:tsu/ba/sa), (anata:a/na/ta), (watashi:wa/ta/shi), (sekai:se/kai), (mirai:mi/rai), (hikari:hi/ka/ri), (ashita:a/shi/ta), (subete:su/be/te), (negai:ne/gai), (kibou:ki/bou), (junpaku:jun/pa/ku), (namida:na/mi/da), (yasashiku:ya/sa/shi/ku), (nemutte:ne/mut/te), (arigatou:a/ri/ga/tou), (sayonara:sa/yo/na/ra), (hajimari:ha/ji/ma/ri), (shinjitsu:shin/ji/tsu), (tsumetai:tsu/me/tai), (kodoku:ko/do/ku), (kurushimi:ku/ru/shi/mi), (itami:i/ta/mi), (omoi:o/moi), (kioku:ki/o/ku), (kodou:ko/dou), (kaze:ka/ze), (sora:so/ra), (hana:ha/na), (kumo:ku/mo), (umi:u/mi), (yoru:yo/ru), (asa:a/sa), (tsuki:tsu/ki), (taiyou:tai/you), (hoshi:ho/shi), (yume:yu/me), (uta:u/ta), (koe:ko/e), (ai:ai), (toki:to/ki), (michi:mi/chi), (tobira:to/bi/ra), (hibiki:hi/bi/ki)

// --- Nhóm 2: Từ có nguyên âm đôi (CVV: ai, oi, ui, ei, au...) ---
(onegai:o/ne/gai), (sekai:se/kai), (tsumetai:tsu/me/tai), (itai:i/tai), (aitai:ai/tai), (kaeritai:ka/e/ri/tai), (shiritai:shi/ri/tai), (kikitai:ki/ki/tai), (ikitai:i/ki/tai), (nemuritai:ne/mu/ri/tai), (aoi:a/oi), (kuroi:ku/roi), (shiroi:shi/roi), (akai:a/kai), (omoi:o/moi), (kurai:ku/rai), (akarui:a/ka/rui), (tsuyoi:tsu/yoi), (yowai:yo/wai), (hayai:ha/yai), (osoi:o/soi), (chikai:chi/kai), (tooi:too/i), (fukai:fu/kai), (asai:a/sai), (takai:ta/kai), (hikui:hi/kui), (kirei:ki/rei), (keikaku:kei/ka/ku), (meiro:mei/ro), (heisei:hei/sei), (eien:ei/en), (sensou:sen/sou), (daisuki:dai/su/ki), (daikirai:dai/ki/rai), (taiketsu:tai/ke/tsu), (saikou:sai/kou), (saiteki:sai/te/ki), (saigo:sai/go), (saisho:sai/sho), (mainichi:mai/ni/chi), (maiasa:mai/a/sa), (kibou:ki/bou), (mirai:mi/rai)

// --- Nhóm 3: Từ có âm ngắt / phụ âm kép (Sokuon: kk, tt, pp, ss, tch...) ---
(nemutte:ne/mut/te), (matte:mat/te), (chotto:chot/to), (kitto:kit/to), (zutto:zut/to), (motto:mot/to), (yatto:yat/to), (kesshite:kes/shi/te), (massugu:mas/su/gu), (ippai:ip/pai), (gakkou:gak/kou), (kekkon:kek/kon), (nikki:nik/ki), (kitte:kit/te), (zasshi:zas/shi), (kissaten:kis/sa/ten), (happi:hap/pi), (shippai:ship/pai), (kekka:kek/ka), (sappari:sap/pa/ri), (hakkiri:hak/ki/ri), (ikkai:ik/kai), (nikkai:nik/kai), (sankai:san/kai), (yonkai:yon/kai), (rokkai:rok/kai), (nakkuru:nak/ku/ru), (shokku:shok/ku), (rokku:rok/ku), (katto:kat/to), (hitto:hit/to), (poketto:po/ket/to), (chiketto:chi/ket/to), (beddo:bed/do)

// --- Nhóm 4: Từ có âm ghép (Yōon: kya, kyu, kyo, sha, shu, sho, cha, chu, cho, nya, hya, mya, rya, gya, ja, bya, pya...) ---
(shunkan:shun/kan), (shoujo:shou/jo), (shounen:shou/nen), (shinjitsu:shin/ji/tsu), (chotto:chot/to), (ryokou:ryo/kou), (yakusoku:ya/ku/so/ku), (kyou:kyou), (ashita:a/shi/ta), (kinou:ki/nou), (densha:den/sha), (jitensha:ji/ten/sha), (kaisha:kai/sha), (jinja:jin/ja), (byouin:byou/in), (byouki:byou/ki), (chuugaku:chuu/ga/ku), (daigaku:dai/ga/ku), (shougaku:shou/ga/ku), (kyoushitsu:kyou/shi/tsu), (ryoushin:ryou/shin), (shashin:sha/shin), (shumi:shu/mi), (ryouri:ryou/ri), (gyuunyuu:gyuu/nyuu), (juusu:juu/su), (chairo:cha/i/ro), (hyaku:hya/ku), (kyaku:kya/ku), (shourai:shou/rai), (shinpai:shin/pai), (junpaku:jun/pa/ku), (tenkyou:ten/kyou)

// --- Nhóm 5: Từ có âm kéo dài (Chōon: ou, uu, aa, ii, ee...) ---
(arigatou:a/ri/ga/tou), (sayounara:sa/you/na/ra), (kibou:ki/bou), (hikouki:hi/kou/ki), (ryokou:ryo/kou), (tokyou:to/kyou), (kyouto:kyou/to), (oosaka:oo/sa/ka), (yuuki:yuu/ki), (yuuhei:yuu/hei), (yuusha:yuu/sha), (juudou:juu/dou), (kuuki:kuu/ki), (suuji:suu/ji), (tsuukin:tsuu/kin), (tsuugaku:tsuu/ga/ku), (okaasan:o/kaa/san), (oniisan:o/nii/san), (oneesan:o/nee/san), (otousan:o/tou/san), (obaasan:o/baa/san), (ojiisan:o/jii/san), (seito:sei/to), (sensei:sen/sei), (gakusei:gaku/sei), (koukou:kou/kou), (chuugakkou:chuu/gak/kou), (daigaku:dai/ga/ku), (eiga:ei/ga), (tokei:to/kei), (yuugata:yuu/ga/ta), (taiyou:tai/you)

// --- Nhóm 6: Từ có âm mũi N độc lập (Hatsuon: n đứng trước phụ âm hoặc cuối từ) ---
(shunkan:shun/kan), (honto:hon/to), (hontou:hon/tou), (onshitsu:on/shi/tsu), (anata:a/na/ta), (kanashimi:ka/na/shi/mi), (tenki:ten/ki), (denki:den/ki), (denwa:den/wa), (nihon:ni/hon), (gohan:go/han), (pan:pan), (hon:hon), (enpitsu:en/pi/tsu), (shinbun:shin/bun), (kaban:ka/ban), (mikan:mi/kan), (ringo:rin/go), (sanpo:san/po), (konban:kon/ban), (zenbu:zen/bu), (zennin:zen/nin), (genki:gen/ki), (tanpen:tan/pen), (ongaku:on/ga/ku), (kankei:kan/kei), (mondai:mon/dai), (undou:un/dou), (anpan:an/pan), (manzai:man/zai)";
    }
}
