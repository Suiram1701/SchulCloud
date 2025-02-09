export function coerceToArrayBuffer(object: string | number[] | Uint8Array | BufferSource): ArrayBuffer {
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

export function coerceToBase64Url(object: number[] | ArrayBuffer | Uint8Array): string {
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