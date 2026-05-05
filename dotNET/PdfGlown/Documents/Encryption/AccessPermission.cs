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
    /**
     * This class represents the access permissions to a document.
     * These permissions are specified in the PDF format specifications, they include:
     * <ul>
     * <li>print the document</li>
     * <li>modify the content of the document</li>
     * <li>copy or extract content of the document</li>
     * <li>add or modify annotations</li>
     * <li>fill in interactive form fields</li>
     * <li>extract text and graphics for accessibility to visually impaired people</li>
     * <li>assemble the document</li>
     * <li>print in degraded quality</li>
     * </ul>
     *
     * This class can be used to protect a document by assigning access permissions to recipients.
     * In this case, it must be used with a specific ProtectionPolicy.
     *
     *
     * When a document is decrypted, it has a currentAccessPermission property which is the access permissions
     * granted to the user who decrypted the document.
     *
     * @see ProtectionPolicy
     * @see org.apache.pdfbox.pdmodel.PDDocument#getCurrentAccessPermission()
     *
     * @author Ben Litchfield
     * @author Benoit Guillon
     *
     */

    public class AccessPermission
    {

        private static readonly int DEFAULT_PERMISSIONS = ~3; //bits 0 & 1 need to be zero
        private static readonly int PRINT_BIT = 3;
        private static readonly int MODIFICATION_BIT = 4;
        private static readonly int EXTRACT_BIT = 5;
        private static readonly int MODIFY_ANNOTATIONS_BIT = 6;
        private static readonly int FILL_IN_FORM_BIT = 9;
        private static readonly int EXTRACT_FOR_ACCESSIBILITY_BIT = 10;
        private static readonly int ASSEMBLE_DOCUMENT_BIT = 11;
        private static readonly int DEGRADED_PRINT_BIT = 12;

        private int bytes;

        private bool readOnly = false;

        /**
		 * Create a new access permission object.
		 * By default, all permissions are granted.
		 */
        public AccessPermission()
        {
            bytes = DEFAULT_PERMISSIONS;
        }

        /**
		 * Create a new access permission object from a byte array.
		 * Bytes are ordered most significant byte first.
		 *
		 * @param b the bytes as defined in PDF specs
		 */

        public AccessPermission(byte[] b)
        {
            bytes = 0;
            bytes |= b[0] & 0xFF;
            bytes <<= 8;
            bytes |= b[1] & 0xFF;
            bytes <<= 8;
            bytes |= b[2] & 0xFF;
            bytes <<= 8;
            bytes |= b[3] & 0xFF;
        }

        /**
		 * Creates a new access permission object from a single integer.
		 *
		 * @param permissions The permission bits.
		 */
        public AccessPermission(int permissions)
        {
            bytes = permissions;
        }

        private bool IsPermissionBitOn(int bit)
        {
            return (bytes & (1 << (bit - 1))) != 0;
        }

        private bool SetPermissionBit(int bit, bool value)
        {
            int permissions = bytes;
            if (value)
            {
                permissions = permissions | (1 << (bit - 1));
            }
            else
            {
                permissions = permissions & (~(1 << (bit - 1)));
            }
            bytes = permissions;

            return (bytes & (1 << (bit - 1))) != 0;
        }

        /**
		 * This will tell if the access permission corresponds to owner
		 * access permission (no restriction).
		 *
		 * @return true if the access permission does not restrict the use of the document
		 */
        public bool isOwnerPermission
        {
            get => (CanAssembleDocument
                    && CanExtractContent
                    && CanExtractForAccessibility
                    && CanFillInForm
                    && CanModify
                    && CanModifyAnnotations
                    && CanPrint
                    && CanPrintDegraded
                    );
        }

        /**
		 * returns an access permission object for a document owner.
		 *
		 * @return A standard owner access permission set.
		 */

        public static AccessPermission GetOwnerAccessPermission()
        {
            return new AccessPermission
            {
                CanAssembleDocument = true,
                CanExtractContent = true,
                CanExtractForAccessibility = true,
                CanFillInForm = true,
                CanModify = true,
                CanModifyAnnotations = true,
                CanPrint = true,
                CanPrintDegraded = true
            };
        }

        /**
		 * This returns an integer representing the access permissions.
		 * This integer can be used for public key encryption. This format
		 * is not documented in the PDF specifications but is necessary for compatibility
		 * with Adobe Acrobat and Adobe Reader.
		 *
		 * @return the integer representing access permissions
		 */

        public int PermissionBytesForPublicKey
        {
            get
            {
                SetPermissionBit(1, true);
                SetPermissionBit(7, false);
                SetPermissionBit(8, false);
                for (int i = 13; i <= 32; i++)
                {
                    SetPermissionBit(i, false);
                }
                return bytes;
            }
        }

        /**
		 * The returns an integer representing the access permissions.
		 * This integer can be used for standard PDF encryption as specified
		 * in the PDF specifications.
		 *
		 * @return the integer representing the access permissions
		 */
        public int PermissionBytes
        {
            get => bytes;
        }

        /**
		 * This will tell if the user can print.
		 *
		 * @return true If supplied with the user password they are allowed to print.
		 */
        /**
		 * Set if the user can print.
		 * <p>
		 * This method will have no effect if the object is in read only mode.
		 *
		 * @param allowPrinting A bool determining if the user can print.
		 */
        public bool CanPrint
        {
            get => IsPermissionBitOn(PRINT_BIT);
            set
            {
                if (!readOnly)
                {
                    SetPermissionBit(PRINT_BIT, value);
                }
            }
        }

        /**
		 * This will tell if the user can modify contents of the document.
		 *
		 * @return true If supplied with the user password they are allowed to modify the document
		 */
        /**
		 * Set if the user can modify the document.
		 * <p>
		 * This method will have no effect if the object is in read only mode.
		 *
		 * @param allowModifications A bool determining if the user can modify the document.
		 */
        public bool CanModify
        {
            get => IsPermissionBitOn(MODIFICATION_BIT);
            set
            {
                if (!readOnly)
                {
                    SetPermissionBit(MODIFICATION_BIT, value);
                }
            }
        }

        /**
		 * This will tell if the user can extract text and images from the PDF document.
		 *
		 * @return true If supplied with the user password they are allowed to extract content
		 *              from the PDF document
		 */
        /**
		 * Set if the user can extract content from the document.
		 * <p>
		 * This method will have no effect if the object is in read only mode.
		 *
		 * @param allowExtraction A bool determining if the user can extract content
		 *                        from the document.
		 */
        public bool CanExtractContent
        {
            get => IsPermissionBitOn(EXTRACT_BIT);
            set
            {
                if (!readOnly)
                {
                    SetPermissionBit(EXTRACT_BIT, value);
                }
            }
        }

        /**
		 * This will tell if the user can add or modify text annotations and fill in interactive forms
		 * fields and, if {@link #canModify() canModify()} returns true, create or modify interactive
		 * form fields (including signature fields). Note that if
		 * {@link #canFillInForm() canFillInForm()} returns true, it is still possible to fill in
		 * interactive forms (including signature fields) even if this method here returns false.
		 *
		 * @return true If supplied with the user password they are allowed to modify annotations.
		 */
        /**
		 * Set if the user can add or modify text annotations and fill in interactive forms fields and,
		 * if {@link #canModify() canModify()} returns true, create or modify interactive form fields
		 * (including signature fields). Note that if {@link #canFillInForm() canFillInForm()} returns
		 * true, it is still possible to fill in interactive forms (including signature fields) even the
		 * parameter here is false.
		 * <p>
		 * This method will have no effect if the object is in read only mode.
		 *
		 * @param allowAnnotationModification A bool determining the new setting.
		 */
        public bool CanModifyAnnotations
        {
            get => IsPermissionBitOn(MODIFY_ANNOTATIONS_BIT);
            set
            {
                if (!readOnly)
                {
                    SetPermissionBit(MODIFY_ANNOTATIONS_BIT, value);
                }
            }
        }

        /**
		 * This will tell if the user can fill in interactive form fields (including signature fields)
		 * even if {@link #canModifyAnnotations() canModifyAnnotations()} returns false.
		 *
		 * @return true If supplied with the user password they are allowed to fill in form fields.
		 */
        /**
		 * Set if the user can fill in interactive form fields (including signature fields) even if
		 * {@link #canModifyAnnotations() canModifyAnnotations()} returns false. Therefore, if you want
		 * to prevent a user from filling in interactive form fields, you need to call
		 * {@link #setCanModifyAnnotations(bool) setCanModifyAnnotations(false)} as well.
		 *<p>
		 * This method will have no effect if the object is in read only mode.
		 *
		 * @param allowFillingInForm A bool determining if the user can fill in interactive forms.
		 */
        public bool CanFillInForm
        {
            get => IsPermissionBitOn(FILL_IN_FORM_BIT);
            set
            {
                if (!readOnly)
                {
                    SetPermissionBit(FILL_IN_FORM_BIT, value);
                }
            }
        }

        /**
		 * This will tell if the user can extract text and images from the PDF document
		 * for accessibility purposes.
		 *
		 * @return true If supplied with the user password they are allowed to extract content
		 *              from the PDF document
		 */
        /**
		 * Set if the user can extract content from the document for accessibility purposes.
		 * <p>
		 * This method will have no effect if the object is in read only mode.
		 *
		 * @param allowExtraction A bool determining if the user can extract content
		 *                        from the document.
		 */
        public bool CanExtractForAccessibility
        {
            get => IsPermissionBitOn(EXTRACT_FOR_ACCESSIBILITY_BIT);
            set
            {
                if (!readOnly)
                {
                    SetPermissionBit(EXTRACT_FOR_ACCESSIBILITY_BIT, value);
                }
            }
        }

        /**
		 * This will tell if the user can insert/rotate/delete pages.
		 *
		 * @return true If supplied with the user password they are allowed to assemble the document.
		 */
        /**
		 * Set if the user can insert/rotate/delete pages.
		 * <p>
		 * This method will have no effect if the object is in read only mode.
		 *
		 * @param allowAssembly A bool determining if the user can assemble the document.
		 */
        public bool CanAssembleDocument
        {
            get => IsPermissionBitOn(ASSEMBLE_DOCUMENT_BIT);
            set
            {
                if (!readOnly)
                {
                    SetPermissionBit(ASSEMBLE_DOCUMENT_BIT, value);
                }
            }
        }

        /**
		 * This will tell if the user can print the document in a degraded format.
		 *
		 * @return true If supplied with the user password they are allowed to print the
		 *              document in a degraded format.
		 */
        /**
		 * Set if the user can print the document in a degraded format.
		 * <p>
		 * This method will have no effect if the object is in read only mode.
		 *
		 * @param canPrintDegraded A bool determining if the user can print the
		 *        document in a degraded format.
		 */
        public bool CanPrintDegraded
        {
            get => IsPermissionBitOn(DEGRADED_PRINT_BIT);
            set
            {
                if (!readOnly)
                {
                    SetPermissionBit(DEGRADED_PRINT_BIT, value);
                }
            }
        }

        /**
		 * This will tell if the object has been set as read only.
		 *
		 * @return true if the object is in read only mode.
		 */
        /**
		* Locks the access permission read only (ie, the setters will have no effects).
		* After that, the object cannot be unlocked.
		* This method is used for the currentAccessPermssion of a document to avoid
		* users to change access permission.
		*/
        public bool IsReadOnly
        {
            get => readOnly;
            set => readOnly = value;
        }


        /**
		 * Indicates if any revision 3 access permission is set or not.
		 * 
		 * @return true if any revision 3 access permission is set
		 */
        public bool HasAnyRevision3PermissionSet
        {
            get
            {
                if (CanFillInForm)
                {
                    return true;
                }
                if (CanExtractForAccessibility)
                {
                    return true;
                }
                if (CanAssembleDocument)
                {
                    return true;
                }
                return CanPrintDegraded;
            }
        }
    }

}