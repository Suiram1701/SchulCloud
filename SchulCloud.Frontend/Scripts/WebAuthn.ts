import { DotNet } from '@microsoft/dotnet-js-interop';
import { coerceToArrayBuffer, coerceToBase64Url } from './Helpers';

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

    async function operationCompleted(objReference: DotNet.DotNetObject, result: AuthenticatorResponse | string): Promise<void> {
        const callbackName = 'operationCompleted';

        if (typeof result !== "string") {
            await objReference.invokeMethodAsync(callbackName, result, null);
        }
        else {
            await objReference.invokeMethod(callbackName, null, result);
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
}