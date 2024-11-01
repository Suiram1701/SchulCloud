import { DotNet } from '@microsoft/dotnet-js-interop';

export namespace WebAuthn {
    type AuthenticatorResponse = {
        id: string,
        rawId: string,
        type: string,
        extensions: AuthenticationExtensionsClientOutputs
    }

    type AttestationResponse = AuthenticatorResponse & {
        response: {
            AttestationObject: string,
            clientDataJSON: string,
            transports: string[]
        }
    }

    type AssertionResponse = AuthenticatorResponse & {
        response: {
            authenticatorData: string,
            clientDataJSON: string,
            signature: string,
            userHandle: string | null
        }
    }

    export function isSupported(): boolean {
        return navigator.credentials instanceof CredentialsContainer;
    }

    export function createCredential(objReference: DotNet.DotNetObject, options: PublicKeyCredentialCreationOptions): AbortController {
        if (options.authenticatorSelection?.authenticatorAttachment === null) {
            options.authenticatorSelection.authenticatorAttachment = undefined;
        }

        // JS interop does convert byte[] into Uint8Array but an ArrayBuffer is required.
        options.challenge = coerceToArrayBuffer(options.challenge);
        options.user.id = coerceToArrayBuffer(options.user.id);
        options.excludeCredentials?.forEach(cred => {
            cred.id = coerceToArrayBuffer(cred.id);
        });

        const abortController: AbortController = new AbortController();
        navigator.credentials.create({
            publicKey: options,
            signal: abortController.signal
        }).then(async credential => {
            if (!(credential instanceof PublicKeyCredential && credential.response instanceof AuthenticatorAttestationResponse)) {
                throw new Error('The authenticator returned an unexpected credential type.');
            }

            const response: AttestationResponse = {
                id: credential.id,
                rawId: coerceToBase64Url(credential.rawId),
                type: credential.type,
                extensions: credential.getClientExtensionResults(),
                response: {
                    AttestationObject: coerceToBase64Url(credential.response.attestationObject),
                    clientDataJSON: coerceToBase64Url(credential.response.clientDataJSON),
                    transports: credential.response.getTransports()
                }
            }

            await operationCompleted(objReference, response);
        }).catch(async error => {
            if (abortController.signal.aborted) {
                return;
            }

            let errorMessage: string = 'An error occurred while creating credential.';
            if (error instanceof Error) {
                errorMessage = error.message;
            }

            await operationCompleted(objReference, errorMessage);
        });

        return abortController;
    }

    export function getCredential(objReference: DotNet.DotNetObject, options: PublicKeyCredentialRequestOptions): AbortController {

        // JS interop does convert byte[] into Uint8Array but an ArrayBuffer is required.
        options.challenge = coerceToArrayBuffer(options.challenge);
        options.allowCredentials?.forEach(cred => {
            cred.id = coerceToArrayBuffer(cred.id);
        });

        const abortController: AbortController = new AbortController();
        navigator.credentials.get({
            publicKey: options,
            signal: abortController.signal
        }).then(async credential => {
            if (!(credential instanceof PublicKeyCredential && credential.response instanceof AuthenticatorAssertionResponse)) {
                throw new Error('The authenticator returned an unexpected credential type.');
            }

            let userHandle: string | null = null;
            if (credential.response.userHandle !== null) {
                userHandle = coerceToBase64Url(credential.response.userHandle);
            }

            const response: AssertionResponse = {
                id: credential.id,
                rawId: coerceToBase64Url(credential.rawId),
                type: credential.type,
                extensions: credential.getClientExtensionResults(),
                response: {
                    authenticatorData: coerceToBase64Url(credential.response.authenticatorData),
                    clientDataJSON: coerceToBase64Url(credential.response.clientDataJSON),
                    signature: coerceToBase64Url(credential.response.signature),
                    userHandle: userHandle
                }
            }
            await operationCompleted(objReference, response);
        }).catch(async error => {
            if (abortController.signal.aborted) {
                return;
            }

            let errorMessage: string = 'An error occurred while getting credentials.';
            if (error instanceof Error) {
                errorMessage = error.message;
            }

            await operationCompleted(objReference, errorMessage);
        });

        return abortController;
    }

    async function operationCompleted(objReference: DotNet.DotNetObject, result: AuthenticatorResponse | string): Promise<void> {
        const callbackName = 'operationCompleted';

        if (typeof result !== "string") {
            await objReference.invokeMethodAsync(callbackName, result, null);
        }
        else {
            await objReference.invokeMethod(callbackName, null, result);
        }
    }

    function coerceToArrayBuffer(object: string | number[] | Uint8Array | BufferSource): ArrayBuffer {
        if (typeof object === "string") {
            // base64url to base64
            object = object.replace(/-/g, "+").replace(/_/g, "/");

            // base64 to Uint8Array
            const str: string = window.atob(object);
            const bytes: Uint8Array = new Uint8Array(str.length);
            for (let i = 0; i < str.length; i++) {
                bytes[i] = str.charCodeAt(i);
            }

            return bytes.buffer;
        }

        // Array to Uint8Array
        if (Array.isArray(object)) {
            return new Uint8Array(object).buffer;
        }

        // Uint8Array to ArrayBuffer
        if (object instanceof Uint8Array) {
            return object.buffer;
        }

        if (object instanceof ArrayBuffer) {
            return object;
        }

        throw new TypeError('Could not coerce "' + typeof object + '" to ArrayBuffer');
    }

    function coerceToBase64Url(object: number[] | ArrayBuffer | Uint8Array): string {
        let bytes: Uint8Array;

        // Array or ArrayBuffer to Uint8Array
        if (Array.isArray(object)) {
            bytes = Uint8Array.from(object);
        }
        else if (object instanceof ArrayBuffer) {
            bytes = new Uint8Array(object);
        }
        else if (object instanceof Uint8Array) {
            bytes = object;
        }
        else {
            throw new Error('Could not coerce "' + typeof object + '" to base64 string');
        }

        // Uint8Array to base64
        let str = "";
        const len = bytes.byteLength;

        for (let i = 0; i < len; i++) {
            str += String.fromCharCode(bytes[i]);
        }
        str = window.btoa(str);

        // base64 to base64url
        // NOTE: "=" at the end of challenge is optional, strip it off here
        return str.replace(/\+/g, "-").replace(/\//g, "_").replace(/=*$/g, "");
    }
}