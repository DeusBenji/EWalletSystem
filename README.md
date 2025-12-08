# 🔐 BachMitID  
Et .NET 9 Web API-projekt, der integrerer **MitID** via **Signicat Sandbox**.  
Formålet er at demonstrere **off-chain alderverifikation** og **privacy-by-design**, hvor kun nødvendige oplysninger udveksles.

---

## 🚀 Login-flow

1. **Start login:**
   👉 [https://localhost:7037/auth/login](https://localhost:7037/auth/login)

2. Der omdirigere til **Signicat MitID Sandbox** for autentificering.

3. Efter vellykket login returneres brugeren til:
   👉 [https://localhost:7037/auth/result](https://localhost:7037/auth/result)

4. API’en svarer med:
   ```json
   {
     "ID": "",
     "sub": "",
     "isAdult": true/false,
   }
    
 5. Der bliver brugt.
 Microsoft.AspNetCore.Authentication.OpenIdConnect, 
 Microsoft.AspNetCore.Authentication.Cookies, 
 Microsoft.IdentityModel.Protocols.OpenIdConnect

 6. 

 🧪 MitID-testbrugere

Opret testidentiteter her:
👉 https://pp.mitid.dk/test-tool/frontend/#/create-identity

docker-compose up -d

For at kører Kafka brug: 
docker-compose up -d
Luk kafka ned med: 
docker-compose down


For visuelt brug - http://localhost:9000/

Eksempel på Gateway kald til MitID konti:
http://localhost:5037/mitid/api/MitIdAccounts
Når du kalder gateway’en i Visual Studio → brug fx
http://localhost:5037/mitid/api/MitIdAccounts

Når du kalder gateway’en i Docker → brug
http://localhost:7005/mitid/api/MitIdAccounts