/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2020
 * lambdasharp.net
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;
using LambdaSharp;
using Microsoft.IdentityModel.Tokens;

namespace Sample.WebSocketsJwt.JwtAuthorizerFunction {

    public sealed class Function : ALambdaFunction<AuthorizationRequest, AuthorizationResponse> {

        //--- Fields ---
        private string _issuer;
        private string _audience;
        private JsonWebKeySet _issuerJsonWebKeySet;

        //--- Methods ---
        public override async Task InitializeAsync(LambdaConfig config) {

            // read configuration settings
            _issuer = config.ReadText("Issuer");
            _audience = config.ReadText("Audience");

            // fetch JsonWebKeySet from issuer
            var jsonWebKeySetUrl = _issuer.EndsWith("/")
                ? _issuer + ".well-known/jwks.json"
                : _issuer + "/.well-known/jwks.json";
            var response = await HttpClient.GetAsync(jsonWebKeySetUrl);
            _issuerJsonWebKeySet = new JsonWebKeySet(await response.Content.ReadAsStringAsync());
        }

        public override async Task<AuthorizationResponse> ProcessMessageAsync(AuthorizationRequest request) {

            // read Authorization header
            var authorization = request.Headers
                .FirstOrDefault(kv => kv.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase))
                .Value
                ?.Trim();
            if(string.IsNullOrEmpty(authorization)) {
                LogInfo("Unauthorized: missing Authorization header");
                throw new Exception("Unauthorized");
            } else {

                // validate JsonWebToken
                try {
                    var claimsPrincipal = new JwtSecurityTokenHandler().ValidateToken(authorization, new TokenValidationParameters {
                        IssuerSigningKeys = _issuerJsonWebKeySet.Keys,
                        ValidIssuer = _issuer,
                        ValidAudience = _audience
                    }, out var _);
                    return new AuthorizationResponse {
                        PrincipalId = "user",
                        PolicyDocument = new PolicyDocument {
                            Statements = {
                                new Statement {
                                    Sid = "JwtAuthorization",
                                    Action = "execute-api:Invoke",
                                    Effect = "Allow",
                                    Resource = request.MethodArn
                                }
                            }
                        }
                    };
                } catch(SecurityTokenExpiredException) {
                    LogInfo("Unauthorized: token expired");
                    throw new Exception("Unauthorized");
                } catch(SecurityTokenInvalidAlgorithmException) {
                    LogInfo("Unauthorized: invalid algorithm");
                    throw new Exception("Unauthorized");
                } catch(SecurityTokenInvalidAudienceException) {
                    LogInfo("Unauthorized: invalid audience");
                    throw new Exception("Unauthorized");
                } catch(SecurityTokenInvalidIssuerException) {
                    LogInfo("Unauthorized: invalid issuer");
                    throw new Exception("Unauthorized");
                } catch(SecurityTokenInvalidLifetimeException) {
                    LogInfo("Unauthorized: invalid lifetime");
                    throw new Exception("Unauthorized");
                } catch(SecurityTokenInvalidSignatureException) {
                    LogInfo("Unauthorized: invalid signature");
                    throw new Exception("Unauthorized");
                } catch(SecurityTokenInvalidSigningKeyException) {
                    LogInfo("Unauthorized: invalid signing keys");
                    throw new Exception("Unauthorized");
                } catch(SecurityTokenInvalidTypeException) {
                    LogInfo("Unauthorized: invalid type");
                    throw new Exception("Unauthorized");
                } catch(SecurityTokenNoExpirationException) {
                    LogInfo("Unauthorized: no expiration");
                    throw new Exception("Unauthorized");
                } catch(SecurityTokenNotYetValidException) {
                    LogInfo("Unauthorized: not yet valid");
                    throw new Exception("Unauthorized");
                } catch(SecurityTokenReplayAddFailedException) {
                    LogInfo("Unauthorized: replay add failed");
                    throw new Exception("Unauthorized");
                } catch(SecurityTokenReplayDetectedException) {
                    LogInfo("Unauthorized: replay detected");
                    throw new Exception("Unauthorized");
                } catch(SecurityTokenValidationException) {
                    LogInfo("Unauthorized: validation failed");
                    throw new Exception("Unauthorized");
                }
            }
        }
    }
}
