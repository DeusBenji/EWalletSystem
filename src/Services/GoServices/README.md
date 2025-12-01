<<<<<<< HEAD
Fabric Resolver Service

Fabric Resolver er en selvstændig microservice, der fungerer som et decentraliseret tillidsanker-lag i projektets arkitektur.
Servicen udstiller et API til:

At oprette og verificere anchors (proofs)

At oprette og resolve DID-dokumenter

At simulere ledger-adfærd via en deterministisk, append‐only mock-implementation

Servicen fungerer i MVP’en som en mocket Hyperledger Fabric ledger, men alle API’er, domænemodeller og dataflows følger samme mønstre som en rigtig Fabric-opkobling.
Dette gør det muligt at demonstrere blockchain‐konceptet uden at være afhængig af en tung Fabric-opstilling til eksamen.

Funktionel oversigt
Anchors

En “anchor” repræsenterer et uforanderligt bevis, der lagres i ledgeren. Et anchor indeholder:

Hash

Issuer DID

BlockNumber

Timestamp

TxID

Metadata (valgfrit)

Anchor-API’et giver mulighed for at:

Oprette et anchor

Læse et anchor

Verificere om et hash eksisterer på ledgeren

Dette svarer til klassiske blockchain-opgaver: proof-of-existence og immutability checks.

DIDs (Decentralized Identifiers)

Servicen kan:

Oprette DID-dokumenter

Resolve DID-dokumenter

Gemme verificeringsnøgler og metadata

Dette følger W3C DID-specifikationens struktur (fx @context, VerificationMethod, Authentication osv.).

Ledger-mode (Mock Fabric)

For at gøre projektet testbart uden en fuld Hyperledger Fabric-installation, kører servicen med en mock ledger:

Alle anchors og DIDs gemmes i deterministiske datastrukturer (map[string]...)

Hvert anchor får:

TxID

BlockNumber

Timestamp

Ledgeren er append-only, og verificeringer er deterministiske

Fabric-klienten kan udskiftes til rigtig Fabric SDK senere uden at ændre andre services

Dette giver en letvægts, reproducerbar og eksamensegnet blockchain-simulator.

Krav

Go 1.21+

Visual Studio Code (anbefalet)

VS Code extension: REST Client (humao.rest-client)

Start servicen
cd fabric-resolver
go run ./cmd/server


Output:

Fabric client initialized (mock mode)
Starting Fabric Resolver on port 8080


Servicen lytter nu på:

http://localhost:8080

Test af service (én klik i VS Code)

Projektet indeholder en REST testfil:

fabric-resolver-tests.http


Testfilen indeholder alle API-kald organiseret i rækkefølge.
Du kan teste hele servicen ved at:

Åbne filen i VS Code

Klikke på Send Request over hver blok

Dette kører:

Health check

Create anchor

Get anchor

Verify anchor

Create DID

Resolve DID

Negative tests (hash ikke findes, DID ikke findes)

=======
Fabric Resolver Service

Fabric Resolver er en selvstændig microservice, der fungerer som et decentraliseret tillidsanker-lag i projektets arkitektur.
Servicen udstiller et API til:

At oprette og verificere anchors (proofs)

At oprette og resolve DID-dokumenter

At simulere ledger-adfærd via en deterministisk, append‐only mock-implementation

Servicen fungerer i MVP’en som en mocket Hyperledger Fabric ledger, men alle API’er, domænemodeller og dataflows følger samme mønstre som en rigtig Fabric-opkobling.
Dette gør det muligt at demonstrere blockchain‐konceptet uden at være afhængig af en tung Fabric-opstilling til eksamen.

Funktionel oversigt
Anchors

En “anchor” repræsenterer et uforanderligt bevis, der lagres i ledgeren. Et anchor indeholder:

Hash

Issuer DID

BlockNumber

Timestamp

TxID

Metadata (valgfrit)

Anchor-API’et giver mulighed for at:

Oprette et anchor

Læse et anchor

Verificere om et hash eksisterer på ledgeren

Dette svarer til klassiske blockchain-opgaver: proof-of-existence og immutability checks.

DIDs (Decentralized Identifiers)

Servicen kan:

Oprette DID-dokumenter

Resolve DID-dokumenter

Gemme verificeringsnøgler og metadata

Dette følger W3C DID-specifikationens struktur (fx @context, VerificationMethod, Authentication osv.).

Ledger-mode (Mock Fabric)

For at gøre projektet testbart uden en fuld Hyperledger Fabric-installation, kører servicen med en mock ledger:

Alle anchors og DIDs gemmes i deterministiske datastrukturer (map[string]...)

Hvert anchor får:

TxID

BlockNumber

Timestamp

Ledgeren er append-only, og verificeringer er deterministiske

Fabric-klienten kan udskiftes til rigtig Fabric SDK senere uden at ændre andre services

Dette giver en letvægts, reproducerbar og eksamensegnet blockchain-simulator.

Krav

Go 1.21+

Visual Studio Code (anbefalet)

VS Code extension: REST Client (humao.rest-client)

Start servicen
cd fabric-resolver
go run ./cmd/server


Output:

Fabric client initialized (mock mode)
Starting Fabric Resolver on port 8080


Servicen lytter nu på:

http://localhost:8080

Test af service (én klik i VS Code)

Projektet indeholder en REST testfil:

fabric-resolver-tests.http


Testfilen indeholder alle API-kald organiseret i rækkefølge.
Du kan teste hele servicen ved at:

Åbne filen i VS Code

Klikke på Send Request over hver blok

Dette kører:

Health check

Create anchor

Get anchor

Verify anchor

Create DID

Resolve DID

Negative tests (hash ikke findes, DID ikke findes)

>>>>>>> 5561c11fc53afde44e4bb3d2ac6610d774f34b80
Resultater vises direkte i editoren.