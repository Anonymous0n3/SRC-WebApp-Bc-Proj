
NAVOD K NASAZENI A SPUSTENI APLIKACE (SRC-WebApp-Bc-Proj)

Tento dokument obsahuje kompletni postup pro sestaveni a spusteni simulatoru namorni radiokomunikace. Aplikace je plne kontejnerizovana, coz zajistuje jeji snadnou prenositelnost a izolaci behoveho prostredi.

1. PREDPOKLADY PRO SPUSTENI
==========================
Pred samotnym spustenim se ujistete, ze mate na hostitelskem stroji (lokalnim pocitaci nebo serveru) nainstalovany nasledujici software:

* Docker (verze 24.0 a novejsi)
* Docker Compose (verze 2.0 a novejsi)
* Git (pro stazeni repozitare)

Poznamka: Na hostitelskem stroji neni nutne mit nainstalovane prostredi .NET, Node.js ani modely Vosk. Vsechny tyto zavislosti se automaticky stahnou a sestavi uvnitr kontejneru.


2. POSTUP NASAZENI
==================

A) Klonovani repozitare:
Nejprve stahnete zdrojove kody projektu na svuj lokalni disk a prejdete do slozky s projektem:
Prikaz: git clone <URL_VASEHO_REPOZITARE>
Prikaz: cd <NAZEV_SLOZKY_PROJEKTU>

B) Priprava datoveho svazku (volitelne):
Aplikace uklada vysledky testovani do textoveho souboru CSV. Pro tento ucel je v docker-compose.yml namapovana slozka ./app_data. Pro predejiti problemum s pravy ji muzete vytvorit rucne:
Prikaz: mkdir app_data
Prikaz: chmod 777 app_data

C) Sestaveni a spusteni kontejneru:
Pro sestaveni obrazu a spusteni celeho ekosystemu na pozadi pouzijte nasledujici prikaz:
Prikaz: docker compose up -d --build

DULEZITE: Prvni sestaveni muze trvat 5 az 15 minut v zavislosti na rychlosti internetu. Stahuji se Docker obrazy (.NET, Node.js) a jazykovy model Vosk (cca 1,8 GB).


3. PRISTUP K APLIKACI
=====================
* Lokalni vyvoj: Aplikace je dostupna na adrese http://localhost
* Produkcni server: Diky kontejneru Caddy je aplikace automaticky vystavena s SSL/TLS certifikatem na vasi domene (napr. https://wea.nti.tul.cz).


4. ZASTAVENI A UDRZBA
=====================
Zastaveni kontejneru:
Prikaz: docker compose down

Aktualizace aplikace (po nahrani noveho kodu):
Prikaz: git pull
Prikaz: docker compose up -d --build


5. KDE NAJDU NASBIRANA DATA?
============================
Veskera testovaci data a analyticke zaznamy uspešnosti aplikace uklada do slozky na hostitelskem stroji:
Cesta: ./app_data/results.csv

Tento soubor lze kdykoliv otevrit nebo zkopirovat bez zasahu do beziciho kontejneru.
