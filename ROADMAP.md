# Snake Reloaded – Megvalósítási terv

A `ToDo.txt` alapján, fázisokra bontva. Minden fázis végén a játék működőképes és commitolható.
A sorrend indoklása: a perkek, az AI, a visszajátszás és a MAUI kliens is ugyanazt igényli —
a játékállapot (világ) és a kirajzolás szétválasztását. Ezért ez az alapozás az első lépés,
és mivel a lista tételeinek fele „beállítható”, a beállításrendszer is korán jön.

---

## 0. fázis – Alapozó refaktor (minden más ennek az előfeltétele)

**Cél:** a játéklogika és a konzolrajzolás szétválasztása, beállításrendszer.

- [x] `GameState` (világ) osztály: játékos kígyó, ellenség-kígyók, kaják, háttér, tick-számláló egy helyen
  (most a `GameEngine.NewGame` lokális változóiban él minden).
- [x] Update és render szétválasztása: a tick csak a világ állapotát módosítja, a renderer a változásokat rajzolja ki.
  `IRenderer` interfész → most egy `ConsoleRenderer` implementálja (a mostani `ConsoleDrawer` + `Background` logika).
- [x] `Settings` osztály + mentés JSON-be (`settings.json`) + Beállítások menüpont.
  Ide kerül majd: pályaméret, sebességek, kaja-cél, szintlépés-küszöb, madár-gyakoriság, easter egg/cheat kapcsolók stb.
- [x] Input-absztrakció: billentyű → játékakció leképezés (később a MAUI swipe ugyanezekre az akciókra fordít).

**Méret:** közepes. Nem ad új funkciót, de minden későbbit olcsóbbá tesz.

## 1. fázis – Aréna és szabályok

- [x] Látható pályahatár-fal (keret rajzolás), **beállítható pályamérettel** (`MapWidth`/`MapHeight` a Settingsből).
- [x] Ellenség-kígyók halandósága: ha bekerítik őket (nincs szabad szomszéd cellájuk), meghalnak → be lehet őket keríteni.
  A tetemük kajává válik (jutalmazza a vadászatot).
- [x] Az ellenség-kígyók is esznek és nőnek.
- [x] Beállítható: kígyók sebessége, hány kaja kell a győzelemhez/szintlépéshez.

**Méret:** kicsi-közepes.

## 2. fázis – AI-szintek és nevesített ellenfelek

- [x] `ISnakeBrain` interfész, nehézségi szintenként egy implementáció:
  | Szint | Viselkedés | Technika |
  |---|---|---|
  | Random | össze-vissza | mostani véletlen kanyarodás |
  | Easy | almára megy | BFS/A* útkeresés a kajához |
  | Normal | kerülget | útkeresés + ütközés-elkerülés (1 lépés előrenézés) |
  | Hard | csapdáz | a játékos várható útvonalának elvágása |
  | Expert | előre számol | több lépéses előrenézés, flood-fill területértékelés, zsákutca-kerülés |
  | Nightmare | együttműködik | közös „tábla”: a többi AI-val összehangolt bekerítés |
- [x] Nehézség választható a menüben/beállításokban.
- [x] Nevesített kígyó-személyiségek (Viper, Ghost, Titan, Fang, Hydra): saját szín, saját agy-paraméterek,
  kedvenc perk, és **perzisztens profil** (`profiles.json`): statisztika, győzelmek, halálozások.
  (A mélyebb, személyiségenkénti AI-stílusok a perkekkel, a 3. fázisban finomodnak.)

**Méret:** nagy (az A* és a flood-fill a lényegi munka, a személyiségek már csak paraméterezés).

## 3. fázis – Szintlépés és perk-rendszer

- [x] Pont- és szintrendszer: kaja → pont, szintlépés beállítható küszöbönként.
- [x] Perk-keretrendszer: `Perk` alaposztály hook-okkal (felvételkor / ütközéskor / tickenként / aktiváló gombra),
  passzív és aktív perkek; az aktiváló billentyűk megjelennek a státuszsorban.
- [x] Roguelike választókártya szintlépéskor: N felkínált perkből választás (N beállítható), a kártyán látszik az aktiváló gomb.
- [x] Perkek hullámokban (egyszerűtől a bonyolultig):
  1. **Egyszerű módosítók:** Páncélos Fej ✓, Diéta ✓, Kettős Érték ✓, Sürgősségi Fék ✓, Berserk ✓
  2. **Világra ható:** Tüskés Farok ✓, Méregcsík/Poison Trail ✓, Time Warp ✓, EMP ✓, Mágneses Vonzás ✓
  3. **Mozgás/fázis:** Fantom Test ✓, Ostorcsapás ✓ (a közeli ellenfelek elülső cellái lecsapódnak, a rövidek meghalnak)
  4. **Környezeti:** Kétéltű ✓, Fakérget Rágó ✓, Kaméleon ✓ — a terep játékelemmé vált:
     a tó lassít (Kétéltű nélkül duplán, azzal 20%-kal gyorsabb), a lombkorona halálos akadály
     (Fakérget Rágóval átjárható), fűben/lomb alatt a vadász-AI-k (Hard/Nightmare) elvesztik a szagot (Kaméleon)
- [x] Ellenség-kígyók is gyűjtenek perkeket, állapotuk mentődik (fejlődő ellenségek).
  Evolúciós szabály: a túlélők megtartják és bővítik a perkjeiket, a halott kígyó elveszti őket.
- [x] Beállítható halál-büntetés: elvesznek-e a perkek; teljes progress-reset opció a beállításokban.
  (A hossz-nullázás kérdése a hossz-átvitellel együtt később.)

**Méret:** nagy, de a keretrendszer után az egyes perkek kicsik — jól darabolható.

## 4. fázis – Kaja-változatok és a madár

- [x] Lucky Food: piros +1, arany +3, lila → perk, kék → pajzs, szivárvány → véletlen effekt (súlyozott spawn).
- [x] Madár-esemény: beállítható gyakorisággal (0 = soha) átrepül egy villogó, csipogó (`Console.Beep`) `V` madár;
  fejjel elkapva azonnali perk-választó kártya. Kockázat–jutalom: a madár után kapkodva könnyű falnak menni.

**Méret:** kicsi-közepes (a perk-kártya a 3. fázisból már megvan).

## 5. fázis – Hangulat és polish

- [x] Háttér-animáció, visszafogottan: víz hullámzik (fázis-eltolt hullámkarakterek/színek), lombok rezegnek.
  (Sodródó felhők/díszmadarak tudatosan kihagyva — a játékmenetbeli madár már megvan.)
- [x] „Snake Reloaded” cím fancy, hullámzó színű felirattal; a menübeli kígyó ide-oda siklik.
- [x] Játék végi ranglista-képernyő: minden kígyó névvel, rangsorban; a leaderboardon 2 soros bejegyzés
  (1. sor alapadatok, 2. sor perkek) — ki a legjobb?
- [x] Hangok: `Console.Beep` dallamok külön szálon (evés, perk, pajzs, halál, győzelem, madár);
  a világ `SoundEvent`-eket bocsát ki, a kliens játssza le — beállításokban kikapcsolható.
- [x] Halálkor az utolsó ~10 másodperc lassított visszajátszása (Game Over képernyőn R billentyű).
- [x] F12 / PrtScr: pillanatnyi állapot mentése fájlba (JSON + „Screenshot” mező a pálya szöveges képével).
  (A PrintScreen billentyűt sok konzol elnyeli, ezért az F12 a biztos gomb.)
- [x] Cheat kódok (god, grow, shrink, perk, spawnbird) begépelve játék közben; beállításokban kikapcsolhatók.
  (Külön easter eggek később.)
- [x] Bug report: kezeletlen kivételnél `mailto:` link megnyitása az exception részleteivel (B billentyű a hibaképernyőn).

**Méret:** közepes, sok kis független tétel — kiválóan tölthető vele a „pihenő” idő nagyobb fázisok közt.

## 6. fázis – Platform-szétválasztás (Core + kliensek)

- [x] Solution átszervezés a ToDo-beli ábra szerint:
  - `Snake.Core` (class library, `SnakeGameEngine.Core`): engine, grid, entitások, AI, perkek, mentés,
    konfiguráció, háttér-modell, szín-matek — platformfüggetlen C#, egyetlen konzolhívás nélkül.
  - A konzol kliens (`Snake/`): ANSI/ASCII megjelenítés (`ConsoleRenderer`, `BackgroundRenderer`),
    billentyűzet, `Console.Beep`, menü — a konzolos feeling megmarad.
  - A 0. fázis `IRenderer`/input-absztrakciója miatt ez főleg fájlmozgatás volt; a `Background`
    rajzolása és a szivárvány-matek szétválasztása kellett még hozzá.
- [x] `Snake.Maui` kliens (v2): GraphicsView (canvas) rajzolás — háttér, kígyók, kaják, madár, méregfelhők,
  halálos lombkorona pirossal körvonalazva; irányítás **swipe + virtuális D-Pad** (mindkettő él);
  perk-választó ActionSheet, **beállítások képernyő** (hang/rezgés, halál esetén perk-vesztés, nehézség,
  cél-hossz, madár-gyakoriság, progress-reset), **aktív perk gombsor** (érintésre aktiválja a perket,
  cooldown a gombon), **rezgés-visszajelzés** (`HapticFeedback`, mivel `Console.Beep` csak Windowson
  létezik — a `SoundEnabled` beállítás mindkettőt vezérli).
  Futtatás Windowson: `dotnet build Snake.Maui/SnakeGameEngine.Maui.csproj -f net10.0-windows10.0.19041.0 -t:Run`
  (a projekt szándékosan nincs a Snake.sln-ben, hogy a sima `dotnet build` gyors maradjon — VS-ből külön megnyitható).
  Még hátravan: Android/iOS build tesztelése valódi eszközön (csak Windowson lett kipróbálva).
- [x] LAN multiplayer (konzol kliens): a roadmap tervezett lépéssorrendje szerint — előbb szinkron-protokoll,
  LAN-nal kezdve, Bluetooth majd rááépülhet. **Host-authoritative** modell: a host futtatja a teljes
  szimulációt (AI, perkek, akadályok — mindez változatlan), és minden tick után egy tömör, csak a
  változásokat tartalmazó JSON „snapshot"-ot küld TCP-n (`Snake.Core/Multiplayer`: `NetworkProtocol`
  hossz-prefixelt keretezéssel, `LanHost`/`LanClient`, `SnapshotBuilder`). A guest nem szimulál semmit,
  csak irányt küld és a kapott snapshotot rajzolja ki (`MultiplayerGuestRenderer`) — ezért nincs
  szétcsúszás-kockázat, mint egy lockstep-modellnél lenne.
  A `GameState` kapott egy második, hálózatilag irányított kígyót (`GuestSnake`, `EnableGuest()`):
  saját ütközés-szabályokkal (fal/önmaga/ellenség/fa/host-kígyó → halál), de perkek nélkül (MVP-egyszerűsítés,
  a host perkjei host-only fogalmak maradnak). Ha a host meghal, a közös meccs véget ér; ha a guest hal
  meg, nézővé válik, a host folytatja. Konzol menü → **M**: Host/Join. Végpontokon átvizsgálva:
  írtam egy ideiglenes, egy processzben futó host+kliens localhost-tesztet a kézfogás/keretezés/
  ütközés-szinkron ellenőrzésére (PASS), majd törölve, nincs a végleges kódban.
  **Ismert korlát:** a MAUI kliens még nem köti be a multiplayert (a protokoll platformfüggetlen a
  Core-ban, szóval megtehető, de a MAUI oldali UI/hálózatkezelés még nincs meg) — ez és a Bluetooth-transzport
  a következő lépés, ha még mélyebbre megy a téma.

**Méret:** nagy. A MAUI kliens önmagában is több ülés.

## 7. fázis – Challenge-rendszer

- [x] Napi kihívások: seedelt napi RNG (a dátum a seed, így mindenkinek ugyanaz a 3 feladata aznap),
  kihívás-definíciók (élj túl 5 percet, ütköztess falnak 3 AI-t, gyűjts 100 almát, ne használj aktív perket
  győzelemig), teljesítés-követés (`dailychallenge.json`, meccseken át összegződik) és -képernyő
  (konzol menü → D). A frissen teljesült feladatok a játék végi képernyőn is megjelennek.

**Méret:** közepes.

## Nem kód jellegű tétel

- YouTube gameplay-videó: felvétel pl. OBS-szel, feltöltés kézzel is mehet (API kulcs nem kell hozzá);
  ha automatizált feltöltés kell, ahhoz Google Cloud projekt + YouTube Data API v3 kulcs szükséges — külön leírást igényel.

---

## Javasolt kezdés

**0. fázis → 1. fázis** sorrendben. A 0. fázis után minden további tétel lényegesen olcsóbb,
az 1. fázis pedig azonnal látványos játékmenet-változást ad (bezárható, halandó, növekvő ellenfelek).
