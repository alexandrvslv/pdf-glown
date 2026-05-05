/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

namespace PdfGlown.Documents.Encryption
{
    /// <summary>
    /// Manages security handlers for the application.
    /// It follows the singleton pattern.
    /// To be usable, security managers must be registered in it.
    /// Security managers are retrieved by the application when necessary.
    /// @author Benoit Guillon
    /// @author John Hewson
    /// </summary>
    public sealed class SecurityHandlerFactory
    {
        /// <summary>Singleton instance</summary>
        public static readonly SecurityHandlerFactory INSTANCE = new SecurityHandlerFactory();
        

        /// <summary>
        /// Returns a new security handler for the given protection policy, or null none is available.
        /// @param policy the protection policy for which to create a security handler
        /// @return a new SecurityHandler instance, or null if none is available
        /// </summary>
        public ISecurityHandler NewSecurityHandlerForPolicy(ProtectionPolicy policy)
        {
            switch (policy)
            {
                case StandardProtectionPolicy standard:
                    return new StandardSecurityHandler(standard);
                case PublicKeyProtectionPolicy publicKey:
                    return new PublicKeySecurityHandler(publicKey);
                default: return null;
            }
        }

        /// <summary>
        /// Returns a new security handler for the given Filter name, or null none is available.
        /// @param name the Filter name from the PDF encryption dictionary
        /// @return a new SecurityHandler instance, or null if none is available
        /// </summary>
        public ISecurityHandler NewSecurityHandlerForFilter(string name)
        {
            switch (name)
            {
                case StandardSecurityHandler.FILTER:
                    return new StandardSecurityHandler();
                case PublicKeySecurityHandler.FILTER:
                    return new PublicKeySecurityHandler();
                default: return null;
            }
        }
        
    }
}