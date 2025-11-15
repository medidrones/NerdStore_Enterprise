using Microsoft.IdentityModel.Tokens;
using NetDevPack.Security.JwtSigningCredentials;
using NetDevPack.Security.JwtSigningCredentials.Interfaces;
using System.Collections.Generic;
using System.Text;

namespace NSE.Identidade.API.Tests.Services.Fake
{
    public class JsonWebKeySetServiceFake : IJsonWebKeySetService
    {
        private readonly SigningCredentials _credentials;
        private readonly List<SecurityKey> _keys;
        private readonly List<JsonWebKey> _jsonWebKeys;

        public JsonWebKeySetServiceFake()
        {
            // Chave simétrica fixa apenas para testes
            var keyBytes = Encoding.ASCII.GetBytes("CHAVE-DE-TESTE-SUPER-SECRETA-1234567890");
            var symmetricKey = new SymmetricSecurityKey(keyBytes);

            _credentials = new SigningCredentials(symmetricKey, SecurityAlgorithms.HmacSha256);

            _keys = new List<SecurityKey>();
            _keys.Add(symmetricKey);

            _jsonWebKeys = new List<JsonWebKey>();
            _jsonWebKeys.Add(new JsonWebKey
            {
                Kty = "oct",
                Kid = "test-key",
                K = Base64UrlEncoder.Encode(keyBytes)
            });
        }

        // =========================================================
        // MÉTODOS EXIGIDOS PELO IJsonWebKeySetService (versão 3.1)
        // =========================================================

        // 1. Retorna a credencial atual (usado pelo AuthenticationService)
        public SigningCredentials GetCurrent()
        {
            return _credentials;
        }

        // 2. Versão GetCurrent com opções
        public SigningCredentials GetCurrent(JwksOptions options)
        {
            return _credentials;
        }

        // 3. Gera chaves (para teste retornamos a mesma)
        public SigningCredentials Generate(JwksOptions options)
        {
            return _credentials;
        }

        // 4. Exige IReadOnlyCollection<JsonWebKey>
        public IReadOnlyCollection<JsonWebKey> GetLastKeysCredentials(int lastKeys)
        {
            return _jsonWebKeys.AsReadOnly();
        }

        // 5. Lista Security Keys
        public IEnumerable<SecurityKey> GetAll()
        {
            return _keys;
        }

        // 6. Adiciona nova chave
        public void AddJwks(string kid, SecurityKey key)
        {
            _keys.Add(key);

            var jsonWebKey = JsonWebKeyConverter.ConvertFromSecurityKey(key);
            jsonWebKey.Kid = kid;

            _jsonWebKeys.Add(jsonWebKey);
        }
    }
}
