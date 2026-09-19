Sen deneyimli bir **Unity oyun geliştiricisi, gameplay programmer, software architect, UI/UX designer ve elektronik devre simülasyonu konusunda bilgili bir teknik tasarımcısın.**

Görevin, aşağıda tanımlanan mobil oyunu **sıfırdan çalışan bir Unity projesi olarak geliştirmek**.

Bu yalnızca bir konsept veya tasarım belgesi oluşturma görevi değildir.

Amaç:

**Projeyi oluşturmak, gerekli sahneleri hazırlamak, scriptleri yazmak, sistemleri birbirine bağlamak, placeholder görsellerle oynanabilir hale getirmek, test etmek ve çalışan bir MVP ortaya çıkarmaktır.**

Eksik veya küçük tasarım kararlarında sürekli benden onay isteme.

Makul ve profesyonel varsayımlar yap, bunları kısa şekilde belirt ve geliştirmeye devam et.

Bir sistemin ilk versiyonu mükemmel olmak zorunda değil.

Öncelik sırası:

1. Çalışması
2. Temiz mimari
3. Genişletilebilirlik
4. Kullanıcı deneyimi
5. Görsel kalite

---

# 1. OYUNUN ANA FİKRİ

Oyun, lise seviyesindeki öğrenciler için tasarlanmış **mobil bir elektronik devre puzzle oyunudur.**

Oyuncunun amacı, kendisine verilen temel elektronik devre elemanlarını kullanarak bir elektrik kaynağından hedef yük veya sisteme elektriği doğru şekilde ulaştırmaktır.

Ancak oyun yalnızca:

“kabloyu A noktasından B noktasına götür”

mantığında olmamalıdır.

Oyuncu gerçek temel elektronik prensiplerini kullanmalıdır.

Örneğin:

* devreyi tamamlamak
* LED yakmak
* ampul çalıştırmak
* doğru direnç kullanmak
* kısa devreden kaçınmak
* doğru polarite kullanmak
* seri bağlantı kurmak
* paralel bağlantı kurmak
* anahtarla yük kontrol etmek
* belirli bir akım aralığını sağlamak
* iki farklı yükü aynı anda çalıştırmak
* kondansatör kullanmak
* devreyi güvenli şekilde çalıştırmak

Oyunun temel eğitim prensibi:

**Oyuncuya cevabı ezberletmek yerine, devrenin neden çalıştığını öğretmek.**

---

# 2. HEDEF KİTLE

Ana hedef kitle:

**14–18 yaş lise öğrencileri.**

Oyuncunun başlangıçta elektronik bilgisi olmayabileceğini varsay.

Oyuncunun ilerleyen bölümlerde şu kavramları anlayabilir hale gelmesini istiyorum:

* açık devre
* kapalı devre
* gerilim
* akım
* direnç
* Ohm Kanunu
* seri bağlantı
* paralel bağlantı
* polarite
* diyot çalışma mantığı
* LED
* kısa devre
* sigorta
* kondansatör
* anahtar
* buton
* temel kontrol devreleri

İleri seviyede:

* potansiyometre
* LDR
* transistör
* röle

eklenebilecek şekilde mimari genişletilebilir olmalıdır.

---

# 3. PLATFORM VE TEKNOLOJİ

Oyunu:

**Unity + C#**

ile geliştir.

Tercihen güncel bir Unity LTS sürümü kullan.

Rendering:

**URP**

kullanılabilir.

Oyun temelde 2D arayüz tabanlıdır.

3D fizik veya karmaşık 3D sahne gerekmiyor.

Hedef platformlar:

* Android
* iOS

Oyun:

**Portrait / dikey mobil ekran**

için tasarlanmalıdır.

Öncelikli referans çözünürlük:

1080x1920.

UI farklı mobil ekran oranlarına uyum sağlamalıdır.

Safe Area desteği düşünülmelidir.

---

# 4. OYUNUN TEMEL GAMEPLAY LOOP'U

Temel döngü:

Level seç

→ kısa görev açıklamasını gör

→ verilen devre elemanlarını incele

→ elemanları çalışma alanına yerleştir

→ terminalleri kablolarla bağla

→ Power butonuna bas

→ devre simüle edilir

→ doğruysa hedef çalışır

→ oyuncuya görsel ve sesli başarı geri bildirimi verilir

→ performans değerlendirilir

→ yıldızlar kazanılır

→ sonraki level açılır.

Devre çalışmıyorsa oyuncu level'dan çıkarılmamalıdır.

Bunun yerine:

* problem gösterilmeli
* neden çalışmadığı açıklanmalı
* oyuncu düzenlemeye devam edebilmelidir.

Deneme-yanılma cezalandırılmamalıdır.

---

# 5. DEVRE EDİTÖRÜ

Ana gameplay ekranında grid tabanlı bir çalışma alanı oluştur.

Oyuncu aşağıdaki component tray / inventory alanından devre elemanlarını sürükleyip çalışma alanına bırakabilsin.

Sistem mobil dokunmatik kullanım düşünülerek tasarlanmalıdır.

Temel etkileşimler:

* drag component
* drop component
* component seçme
* component taşıma
* component döndürme
* component silme
* terminal seçme
* terminalden terminale kablo çizme
* kablo silme
* undo
* reset circuit
* power on/off

Grid sistemi elemanların temiz hizalanmasını sağlamalıdır.

Ancak mimari daha sonra serbest yerleşim sistemine geçilebilecek kadar bağımsız olmalıdır.

---

# 6. TERMINAL VE KABLO SİSTEMİ

Her elektronik elemanın açık şekilde tanımlanmış terminalleri olmalıdır.

Örneğin:

Resistor:
Terminal A
Terminal B

LED:
Anode
Cathode

Battery:
Positive
Negative

Switch:
Input
Output

Oyuncu bir terminale dokunup başka terminale sürükleyerek kablo oluşturabilmelidir.

Kabloların elektriksel bağlantıları yalnızca görsel olmamalıdır.

Circuit simulation sistemi tarafından gerçekten node bağlantısı olarak kullanılmalıdır.

Devre:

**graph/node tabanlı**

bir yapıyla temsil edilmelidir.

---

# 7. DEVRE SİMÜLASYONUNUN ANA PRENSİBİ

Oyunun en önemli teknik özelliklerinden biri:

**Puzzle'ların çözümü önceden belirlenmiş bağlantı kombinasyonları üzerinden kontrol edilmemelidir.**

Örneğin:

if wire1 == correctWire && resistorPosition == correctPosition

gibi hard-coded puzzle çözüm sistemlerinden kaçın.

Bunun yerine oyuncunun oluşturduğu gerçek bağlantılardan devre grafiği üret.

Devre simülasyon motoru:

* hangi node'ların bağlı olduğunu
* açık/kapalı bağlantıları
* kaynakları
* yükleri
* dirençleri
* polariteyi
* yaklaşık akımı
* yaklaşık gerilim düşümlerini

hesaplamalıdır.

Sonrasında level objective sistemi:

“Bu LED gerçekten çalışıyor mu?”

gibi sonuçları değerlendirmelidir.

Böylece geliştiricinin önceden tanımlamadığı fakat elektriksel olarak doğru alternatif çözümler de kabul edilmelidir.

---

# 8. SİMÜLASYON GERÇEKÇİLİK SEVİYESİ

Tam kapsamlı profesyonel SPICE simülatörü yazmaya çalışma.

Bu bir lise seviyesi puzzle oyunudur.

Elektrik modeli:

**eğitim açısından doğru fakat oyun açısından sadeleştirilmiş**

olmalıdır.

İlk sürüm ağırlıklı olarak DC devreleri desteklemelidir.

Desteklenecek temel hesaplamalar:

Ohm Kanunu:

V = I × R

I = V / R

R = V / I

Seri dirençler:

Rtotal = R1 + R2 + ...

Basit paralel direnç hesapları.

Gerilim düşümleri.

Branch akımları.

Kaynak polaritesi.

LED forward voltage.

Akım sınırları.

Short circuit detection.

Open circuit detection.

Switch state.

İleri fiziksel etkiler gereksizdir.

Örneğin:

* sıcaklık katsayıları
* gerçek yarı iletken eğrileri
* parasitic capacitance
* electromagnetic effects

ilk sürümde gerekli değildir.

---

# 9. ELEKTRONİK ELEMAN SİSTEMİ

Component sistemini genişletilebilir yap.

Mümkünse ortak bir:

CircuitComponent

base class veya interface tasarla.

Component verileri ScriptableObject üzerinden tutulabilir.

Her component şu tür verilere sahip olabilsin:

* component ID
* component type
* display name
* description
* educational description
* sprite
* terminal definitions
* electrical parameters
* rotation support
* current state
* runtime measurements

İlk MVP'de şu elemanları destekle:

1. Wire
2. Battery / DC Voltage Source
3. Resistor
4. Switch
5. Push Button
6. LED
7. Light Bulb
8. Diode
9. Fuse
10. Capacitor

Mimari daha sonra şu componentlerin eklenmesini kolaylaştırmalıdır:

* Potentiometer
* LDR
* Thermistor
* Relay
* Transistor
* Motor
* Buzzer
* Logic components

---

# 10. KOMPONENT DAVRANIŞLARI

## Battery

Gerilim değeri bulunmalı.

Örneğin:

1.5 V
5 V
9 V
12 V

Positive ve Negative terminal içermeli.

---

## Resistor

Resistance değeri Ohm cinsinden tutulmalı.

Örneğin:

100 Ω
220 Ω
330 Ω
1 kΩ

---

## LED

Anode ve Cathode terminali bulunmalı.

Forward voltage değeri bulunmalı.

Örneğin yaklaşık:

2 V.

LED yalnızca:

* doğru polaritedeyse
* yeterli forward voltage varsa
* akım uygun aralıktaysa

yanmalıdır.

Aşırı akım varsa LED hasar görmüş gibi gösterilebilir.

İlk versiyonda hasar kalıcı olmak zorunda değildir.

---

## Switch

Open / Closed state içermeli.

Oyuncu üzerine dokunarak state değiştirebilmeli.

---

## Push Button

Sadece basılı tutulduğu veya aktive edildiği süre boyunca closed olabilir.

---

## Fuse

Maksimum current rating bulunmalı.

Bu sınır aşılırsa fuse blow state'e geçmeli ve devreyi açmalıdır.

Reset edildiğinde eski haline dönebilir.

---

## Diode

Akımın yalnızca bir yönde geçmesine izin vermeli.

---

## Light Bulb

Belirli gerilim ve akım aralığında çalışmalıdır.

---

## Capacitor

İlk sürümde sadeleştirilmiş capacitor modeli kullan.

Charge state:

0–1

veya Coulomb bazlı sade bir state olabilir.

Zaman içinde şarj ve deşarj davranışı göster.

Tam fiziksel RC simülasyonu zorunlu değil fakat eğitim açısından tutarlı olmalıdır.

---

# 11. ELEKTRİĞİN GÖRSELLEŞTİRİLMESİ

Power aktif olduğunda çalışan kablolar üzerinde elektrik akışını temsil eden küçük hareketli görsel parçacıklar veya animasyonlar kullan.

Bu görsel:

“elektronların fiziksel hızı budur”

gibi yanlış bir eğitim mesajı vermemelidir.

Sadece:

**current flow visualization**

olarak kullanılmalıdır.

Current flow arttıkça animasyon yoğunluğu veya hızı biraz değişebilir.

Aktif bağlantılar:

hafif glow

ile gösterilebilir.

Sorunlu kablolar veya componentler:

kırmızı / turuncu uyarı efektiyle

vurgulanabilir.

---

# 12. POWER MODE

Oyunun iki temel modu olsun:

EDIT MODE

ve

POWER MODE.

Edit Mode:

Oyuncu eleman yerleştirir ve kablo çizer.

Power butonuna basınca:

Power Mode aktif olur.

Circuit simulation çalışır.

Elektriksel sonuçlar hesaplanır.

Componentler state değiştirir.

Örneğin:

* LED yanar
* bulb yanar
* fuse atar
* capacitor şarj olur

Oyuncu Power butonuna tekrar basarak edit mode'a dönebilir.

---

# 13. HATA GERİ BİLDİRİMİ

Oyuncunun yaptığı hataları öğretici şekilde açıklayan bir Diagnostic System oluştur.

Örnek mesajlar:

“Circuit is open. The current has no complete path back to the battery.”

“The LED is connected with reversed polarity.”

“The LED current is too high. Try adding a resistor.”

“The switch is currently open.”

“The fuse has blown because the current exceeded its rating.”

“There is a short circuit across the power source.”

Mesajlar:

* kısa
* anlaşılır
* lise seviyesine uygun

olmalıdır.

Mümkünse hata ile ilgili component çalışma alanında highlight edilmelidir.

---

# 14. LEVEL OBJECTIVE SİSTEMİ

Level sistemini yalnızca tek hedef türüne bağlama.

Modüler bir Objective sistemi oluştur.

Örneğin:

ActivateComponentObjective

MaintainCurrentRangeObjective

MaintainVoltageRangeObjective

PreventShortCircuitObjective

UseSpecificComponentObjective

LimitComponentCountObjective

MultipleLoadsActiveObjective

TimedObjective

gibi objective türleri desteklenebilir.

Level birden fazla objective içerebilsin.

Örneğin:

Primary Objective:

Light both LEDs.

Secondary Objective:

Do not exceed 20 mA.

Efficiency Objective:

Use maximum 2 resistors.

---

# 15. LEVEL DATA

Level tasarımlarını mümkün olduğunca code içine hard-code etme.

ScriptableObject veya JSON benzeri data-driven bir yapı kullan.

Her level şu bilgileri taşıyabilsin:

* ID
* chapter
* title
* description
* educational concept
* grid size
* initial fixed components
* available components
* quantities
* objectives
* star conditions
* tutorial state
* hints
* success message

Böylece yeni level eklemek için gameplay kodunu değiştirmek gerekmemelidir.

---

# 16. YILDIZ SİSTEMİ

Her level 3 yıldız verebilir.

Önerilen yapı:

1 yıldız:
Ana objective tamamlandı.

2 yıldız:
Component usage hedefi karşılandı.

3 yıldız:
Efficiency veya optimization hedefi karşılandı.

Ancak oyuncunun progression için yalnızca 1 yıldız alması yeterlidir.

Oyuncu istediği zaman eski level'a dönüp daha iyi çözüm deneyebilmelidir.

---

# 17. HINT SİSTEMİ

Her level için üç seviyeli hint sistemi tasarla.

Hint 1:

Kavramsal yardım.

Örneğin:

“An LED needs current limiting.”

Hint 2:

Hangi componentin gerekli olduğunu belirt.

Örneğin:

“Try placing a resistor in series with the LED.”

Hint 3:

Çalışma alanında olası doğru bağlantılardan veya pozisyonlardan birini highlight et.

Tam çözümü otomatik kurma.

---

# 18. EĞİTİM SİSTEMİ

Oyun bir ders kitabına dönüşmemelidir.

Uzun metin ekranlarından kaçın.

Yeni bir elektronik kavram ilk kez geldiğinde:

10–30 saniyelik küçük interaktif öğretim kullan.

Örneğin:

Chapter 2'de ilk resistor level'ında:

“Resistance limits current.”

Ardından oyuncuya:

Battery → LED

devresi göster.

LED aşırı akımdan zarar görsün.

Daha sonra resistor ver.

Oyuncu resistor ekleyerek problemi çözsün.

Bu şekilde:

**show → fail → understand → solve**

öğretim döngüsü kullanılabilir.

---

# 19. CHAPTER YAPISI

Oyunun uzun vadeli progression sistemi:

## Chapter 1 — Circuit Basics

* battery
* wire
* bulb
* switch
* open circuit
* closed circuit

## Chapter 2 — Voltage & Current

* voltage
* current
* basic measurement

## Chapter 3 — Resistance & Ohm's Law

* resistor
* current limiting
* V = I × R

## Chapter 4 — Series & Parallel

* series circuits
* parallel circuits
* branch current

## Chapter 5 — LEDs & Diodes

* polarity
* forward voltage
* current limiting
* diode direction

## Chapter 6 — Capacitors

* charge
* discharge
* simple timing puzzles

## Chapter 7 — Switches & Control

* switch
* push button
* multiple control paths

## Chapter 8 — Sensors

* LDR
* potentiometer
* basic sensing logic

## Chapter 9 — Transistors & Relays

* simple transistor switching
* relay control

## Chapter 10 — Final Circuit Challenges

Önceki tüm sistemlerin kombinasyonu.

MVP sırasında Chapter 1–3 tamamen oynanabilir hale getirilmeli.

---

# 20. MVP LEVEL SAYISI

İlk çalışan MVP için:

yaklaşık 20–25 level hedefle.

İlk prototype aşamasında tüm 25 level'ı hemen yapmak zorunda değilsin.

Önce:

5–10 kaliteli level

ile core sistemlerin çalıştığını kanıtla.

Sonrasında aynı sistem üzerinden level sayısını artır.

---

# 21. ÖRNEK İLK LEVELLAR

## Level 1

Goal:

Light the bulb.

Available:

* battery
* bulb
* wires

Amaç:

Kapalı devre öğretmek.

---

## Level 2

Goal:

Turn the bulb on using a switch.

Available:

* battery
* bulb
* switch
* wires

---

## Level 3

Goal:

Repair the broken circuit.

Bazı bağlantılar hazır olabilir.

Oyuncu eksik bağlantıyı bulur.

---

## Level 4

Goal:

Light two bulbs.

Series bağlantı giriş seviyesi.

---

## Level 5

Goal:

Light two bulbs independently.

Parallel connection tanıtılır.

---

## Level 6

LED tanıtılır.

Oyuncu ters polarite problemiyle karşılaşır.

---

## Level 7

LED doğru bağlanır fakat aşırı current vardır.

Oyuncunun resistor eklemesi gerekir.

---

## Level 8

Farklı resistor değerlerinden doğru olan seçilir.

---

## Level 9

Ohm Law üzerinden basit current objective.

Örneğin:

Keep current between 15 mA and 20 mA.

---

## Level 10

Seri ve paralel bağlantıların birlikte kullanıldığı mini challenge.

---

# 22. SANDBOX MODE

Oyunda ileride açılan bir Sandbox Mode tasarla.

Sandbox'ta:

* sınırsız component
* serbest devre tasarımı
* objective yok
* measurement tools
* power simulation

bulunmalıdır.

MVP sırasında Sandbox'ın temel versiyonu yapılabilir.

Sandbox mode simulation sisteminin gerçekten genel çalıştığını test etmek için de kullanılmalıdır.

---

# 23. MULTIMETER

İleri aşamada fakat mimari başlangıçtan destekleyecek şekilde:

Multimeter tool tasarla.

Oyuncu:

Voltage Mode

ve

Current Mode

seçebilsin.

Voltage ölçümü:

iki node seçerek.

Current ölçümü:

bir branch/component üzerinden.

İlk MVP'de basit voltage probe sistemi yeterlidir.

---

# 24. UI YAPISI

Ana menü:

Play

Sandbox

Settings

About / Learn

Level Selection ekranı:

chapter kartları

ve level node'ları.

Gameplay ekranı:

Üst bölüm:

* level title
* objective

Orta:

* circuit workspace

Alt bölüm:

* component tray

Alt veya sağ tarafta:

* Power
* Undo
* Reset
* Hint

Component seçildiğinde küçük bilgi paneli gösterilebilir.

Örneğin:

330 Ω Resistor

“Limits current flow.”

---

# 25. GÖRSEL TARZ

Stil:

**clean modern electronics laboratory + PCB aesthetic**

olmalıdır.

Çocuk oyunu gibi görünmesin.

Aşırı ciddi mühendislik yazılımı gibi de görünmesin.

Hedef:

modern mobil puzzle oyunu.

Componentler:

yarı fiziksel, yarı stilize edilmiş olabilir.

Örneğin resistor gerçek resistor renklerine benzeyebilir fakat terminalleri ve bağlantı noktaları daha okunabilir şekilde vurgulanabilir.

Placeholder asset kullanmak gerekiyorsa:

basit ama düzenli geometrik şekiller ve ikonlar kullan.

Gameplay'i asset olmadığı için durdurma.

---

# 26. UX PRENSİPLERİ

Mobil kullanımda:

* büyük dokunma alanları
* minimum yanlış dokunma
* rahat drag & drop
* terminal snapping
* visual feedback
* kısa animasyonlar

kullan.

Oyuncu bir terminale yakın kablo bıraktığında otomatik snap uygulanabilir.

Component seçildiğinde terminal noktaları görünür hale gelsin.

---

# 27. SES

Basit placeholder audio sistemi oluştur.

Gerekli eventler:

* component pick
* component place
* wire connect
* switch click
* power on
* electrical warning
* success
* star earned

Ses dosyaları hazır değilse sistemi event tabanlı hazırla ve placeholder kullan.

---

# 28. SAVE SYSTEM

Offline local save kullan.

Kaydedilecek veriler:

* unlocked chapters
* completed levels
* best stars
* tutorials completed
* settings
* sandbox unlock

JSON veya PlayerPrefs + serialized data kullanılabilir.

Save sistemini ayrı servis olarak tasarla.

---

# 29. OFFLINE ÇALIŞMA

Oyun gameplay için internet bağlantısı gerektirmemelidir.

Core systems tamamen offline çalışmalıdır.

Backend oluşturma.

---

# 30. MONETIZATION

MVP'de reklam veya ödeme sistemi implement etme.

Mimari ileride:

free first chapters

*

one-time full unlock

modeline uyumlu olabilir.

Gameplay içinde enerji sistemi, zorunlu reklam veya bekleme timer'ı oluşturma.

---

# 31. KOD MİMARİSİ

Kodun tek birkaç dev MonoBehaviour içine yığılmasını istemiyorum.

Mantıklı separation of concerns kullan.

Örneğin:

Core

Circuit

Components

Simulation

Levels

Objectives

UI

Save

Tutorial

Audio

Utilities

gibi klasörler oluştur.

Örnek ana sistemler:

CircuitBoard

CircuitNode

CircuitConnection

CircuitComponent

CircuitSimulator

SimulationResult

LevelManager

LevelDefinition

ObjectiveSystem

HintSystem

TutorialManager

SaveManager

AudioManager

UIManager

gibi yapılara bölünebilir.

İsimleri gerektiğinde daha iyi şekilde değiştirebilirsin.

---

# 32. CIRCUIT GRAPH MODEL

Circuit board runtime sırasında:

Nodes

Components

Connections

üzerinden temsil edilmelidir.

Her terminal bir electrical node veya node reference içermelidir.

Wire aynı electrical potential node altında terminalleri birleştirebilir veya graph connection olarak tutulabilir.

Simulation başlamadan önce graph normalize edilebilir.

Örneğin Union-Find / Disjoint Set gibi bir sistem aynı elektrik node'una bağlı terminalleri tespit etmek için kullanılabilir.

Uygun algoritmayı kendin seç.

Ancak kod:

* anlaşılır
* test edilebilir
* genişletilebilir

olsun.

---

# 33. SIMULATION RESULT

Simulation sistemi yalnızca component animasyonlarını direkt değiştirmesin.

Önce bir SimulationResult üret.

Örneğin:

Node voltages

Branch currents

Component states

Warnings

Errors

Objective-relevant values

SimulationResult üzerinden:

visual systems

ve

objective systems

beslensin.

Böylece simulation ile presentation birbirinden ayrılmış olur.

---

# 34. DIAGNOSTIC SYSTEM

Simulation sırasında:

Severity seviyeleri oluştur.

Örneğin:

Info

Warning

Error

Possible diagnostic types:

OpenCircuit

ShortCircuit

ReversePolarity

OverCurrent

UnderVoltage

FuseBlown

FloatingNode

InvalidConnection

Diagnostic sonuçları UI tarafından gösterilebilsin.

---

# 35. TESTLER

Elektrik simülasyon sistemi için mümkün olduğunca Unity Test Framework kullan.

Özellikle şu testleri yaz:

Battery + resistor simple circuit

Series resistors

Parallel resistors

Open circuit

Short circuit

Reversed diode

LED with valid resistor

LED overcurrent

Switch open

Switch closed

Fuse normal operation

Fuse overcurrent

Bu core simulation sistemi kritik olduğu için yalnızca manuel testlere güvenme.

---

# 36. PERFORMANS

Bu oyun için aşırı optimization gerekli değildir.

Ancak mobil hedefli olduğu için:

* gereksiz Update kullanımından kaçın
* event-driven yaklaşımı tercih et
* object allocationlarını kontrol altında tut
* pooling gereken görsel efektlerde kullanılabilir

Simulation yalnızca:

Power aktif edildiğinde

veya devre değiştiğinde

tekrar hesaplanabilir.

Her frame full circuit simulation yapma.

---

# 37. ACCESSIBILITY

Şunları düşün:

* renk körlüğü nedeniyle yalnızca renge bağlı feedback kullanma
* sembol + renk kullan
* readable fonts
* yeterli contrast
* vibration açma/kapama
* sound açma/kapama

---

# 38. DİL

İlk implementasyonda UI metinleri İngilizce olabilir.

Ancak bütün metinleri kodun içine dağınık string olarak yazma.

Localization sistemine geçilebilecek merkezi bir yapı kullan.

İleride:

English

Turkish

desteklenebilmelidir.

---

# 39. GELİŞTİRME SIRASI

Projeyi aşağıdaki sırayla geliştir.

## PHASE 1 — Project Foundation

Unity project setup.

Scenes.

Folder structure.

Basic navigation.

Portrait UI.

---

## PHASE 2 — Circuit Editor

Grid.

Drag/drop.

Components.

Terminals.

Wire connections.

Delete.

Rotate.

Reset.

---

## PHASE 3 — Circuit Data Model

Nodes.

Connections.

Component electrical model.

Graph generation.

---

## PHASE 4 — Basic DC Simulation

Battery.

Wire.

Resistor.

Open circuit.

Closed circuit.

Voltage.

Current.

---

## PHASE 5 — Interactive Components

Switch.

Bulb.

LED.

Diode.

Fuse.

---

## PHASE 6 — Puzzle System

Level definitions.

Objectives.

Success detection.

Stars.

Hints.

---

## PHASE 7 — Education Layer

Tutorial.

Diagnostics.

Component descriptions.

---

## PHASE 8 — Progression

Level select.

Unlocking.

Save system.

---

## PHASE 9 — Polish

Animations.

Particle current flow.

Audio.

Haptics.

UI feedback.

---

## PHASE 10 — Additional Systems

Capacitor.

Sandbox.

Measurement tools.

Additional levels.

---

# 40. ÇALIŞMA PRENSİBİN

Geliştirirken yalnızca bana uzun açıklamalar verip kod yazmayı bırakma.

Asıl hedef:

**çalışan proje.**

Her aşamada gerçekten gerekli dosyaları oluştur.

Kod yaz.

Scene setup yap.

Prefab gerekiyorsa oluştur.

ScriptableObject gerekiyorsa oluştur.

UI bağlantılarını yap.

Mümkün olduğunca sistemleri çalışır halde bırak.

---

# 41. HATA YÖNETİMİ

Bir compile error veya runtime error oluşursa:

görmezden gelip sonraki adıma geçme.

Önce nedeni bul.

Düzelt.

Tekrar kontrol et.

Sonrasında geliştirmeye devam et.

---

# 42. PLACEHOLDER ASSET KURALI

Görsel veya ses asset'i eksik diye geliştirmeyi durdurma.

Basit placeholder kullan.

Örneğin:

Battery = rectangle + + / -

Resistor = rectangular component

LED = circle

Terminal = small connector node

gibi.

Daha sonra gerçek assetlerle değiştirilebilecek şekilde prefab sistemi oluştur.

---

# 43. OVERENGINEERING YAPMA

İlk günden profesyonel SPICE simulator veya yüzlerce component destekleyen bir framework yazmaya çalışma.

Ama aynı zamanda sistemi:

“bu yalnızca bu 10 level için çalışır”

şeklinde de kodlama.

Amaç:

**küçük fakat sağlam ve genişletilebilir bir simulation engine.**

---

# 44. BAŞARI KRİTERLERİ

İlk MVP şu durumda başarılı kabul edilir:

Oyuncu mobil uyumlu gameplay ekranına girebilir.

Componentleri grid üzerine sürükleyebilir.

Terminaller arasında kablo oluşturabilir.

Battery kullanarak devreye güç verebilir.

Gerçek bağlantılardan circuit graph oluşturulur.

Resistor üzerinden yaklaşık current hesaplanır.

Bulb ve LED doğru durumda çalışır.

LED polaritesi kontrol edilir.

Open circuit tespit edilir.

Short circuit tespit edilir.

Switch devreyi açıp kapatır.

Diagnostic message gösterilir.

Birden fazla geçerli devre çözümü objective tarafından kabul edilir.

En az 5–10 level oynanabilir.

Level tamamlanır.

Stars verilir.

Progress kaydedilir.

Sonraki level açılır.

---

# 45. ÖNEMLİ TASARIM PRENSİBİ

Bu oyun:

“doğru parçayı doğru kutuya sürükle”

oyunu olmamalıdır.

Oyuncuya gerçek bir küçük elektronik sistem vermelidir.

Oyuncu:

“Bu devre neden çalışmıyor?”

diye düşünmelidir.

Sonrasında problemi bulup:

“Direnç koymazsam LED'den fazla akım geçiyor.”

“Bu switch devreyi açık bırakıyor.”

“LED'i ters bağladım.”

“Burada paralel bağlantı gerekiyor.”

gibi gerçek elektronik prensiplerini kendisi keşfetmelidir.

Puzzle tasarımındaki en önemli hedef budur.

---

# 46. SON TALİMAT

Şimdi projeyi geliştirmeye başla.

Önce mevcut proje durumunu incele.

Eğer boş bir Unity projesiyse gerekli yapıyı sıfırdan kur.

Sürekli benden mikro kararlar için onay isteme.

Belirsiz küçük detaylarda:

1. mobil puzzle oyunları için en iyi UX'i,
2. lise düzeyi elektronik eğitim doğruluğunu,
3. temiz ve genişletilebilir Unity mimarisini

önceliklendirerek karar ver.

Her büyük aşamanın sonunda:

* ne oluşturduğunu,
* hangi dosyaları eklediğini,
* sistemin şu anda ne yapabildiğini,
* varsa bilinen eksikleri

kısaca belirt.

Ancak rapor yazmaya kod yazmaktan daha fazla zaman harcama.

**Ana hedef çalışan, oynanabilir ve genişletilebilir bir elektronik puzzle oyunu oluşturmaktır.**
