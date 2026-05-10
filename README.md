# Baterija_59

## Opis projekta

Projekat implementira WCF sistem za analizu i upravljanje podacima o Li-ion baterijama.

Klijent učitava EIS CSV podatke i šalje ih sekvencijalno WCF servisu korišćenjem `netTcpBinding` komunikacije.  
Servis validira podatke, čuva ih na disk i vrši analitiku nad naponom i impedansom.

---

## Arhitektura sistema

![Architecture](Documentation/Architecture.png)

---

## Sekvencijalni tok komunikacije

![Sequence](Documentation/Sequence.png)

---

## Korišćene tehnologije

- C#
- .NET Framework 4.8
- WCF
- netTcpBinding
- CSV File I/O
- IDisposable pattern

---

## Funkcionalnosti

- StartSession / PushSample / EndSession komunikacija
- CSV parsing
- Sekvencijalni streaming podataka
- Validacija podataka
- session.csv i rejects.csv generisanje
- invalid_rows_log.txt logovanje
- Delegati i događaji
- VoltageSpike detekcija
- ImpedanceJump detekcija
- Running Mean analiza

---

## Struktura projekta

```txt
Baterija_59/
│
├── Baterija_59
│   ├── Dataset
│   ├── Documentation
│   ├── Events
│   ├── Faults
│   ├── IO
│   ├── Models
│   └── TestData
│
├── Baterija_59.Client
│
└── Baterija_59.Host
```

---

## Pokretanje projekta

1. Pokrenuti prvo `Baterija_59.Host`
2. Pokrenuti `Baterija_59.Client`
3. U klijentu izabrati jednu od dve opcije:
   - Opcija 1 → slanje jednog CSV fajla
   - Opcija 2 → slanje kompletnog dataset foldera

### Primer putanje za jedan CSV fajl

```txt
...\Baterija_59\TestData\test_spike.csv
```

### Primer putanje za dataset

```txt
...\Baterija_59\Dataset
```

---

## Dataset struktura

Dataset preuzet sa:

https://data.mendeley.com/datasets/cb887gkmxw/2

Korišćena struktura:

```txt
Dataset/
└── B01/
    └── EIS measurements/
        └── Test_1/
            └── Hioki/
```

---

## WCF komunikacija

Klijent koristi sledeće operacije:

- `StartSession(EisMeta meta)`
- `PushSample(EisSample sample)`
- `EndSession()`

Komunikacija koristi `netTcpBinding`.

---

## Analitika i događaji

Sistem detektuje:

- naglu promenu napona (`ΔV`)
- naglu promenu impedanse (`ΔZ`)
- odstupanje od tekućeg proseka impedanse

Implementirani događaji:

- `OnTransferStarted`
- `OnSampleReceived`
- `OnTransferCompleted`
- `OnWarningRaised`

---

## Izlazni fajlovi

Servis generiše:

```txt
session.csv
rejects.csv
invalid_rows_log.txt
```

Primer strukture:

```txt
Data/
└── B01/
    └── Test_1/
        └── 50/
            ├── session.csv
            └── rejects.csv
```

---

## Primer izlaza

```txt
[EVENT] Transfer started: test_spike.csv

[WARNING] VoltageSpike | Detektovana nagla promena napona.

[WARNING] ImpedanceJump | Detektovana nagla promena impedanse.

[WARNING] OutOfBandWarning | Impedansa odstupa od tekuceg proseka.

[EVENT] Transfer completed: test_spike.csv
```

---

## Autori

- Aleksa Mrđa — PR116/2022
- Ivan Dobretić — PR138/2022
