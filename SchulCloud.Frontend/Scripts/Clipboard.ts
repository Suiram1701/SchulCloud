import { DotNetStreamReference } from "./Declarations/DotnetStreamReference";

export namespace Clipboard {
    export function isSupported(): boolean {
        return navigator.clipboard instanceof globalThis.Clipboard;
    }

    export async function write(value: string | DotNetStreamReference, type: string | null): Promise<string | null> {
        let blob: Blob;
        if (typeof value === "string") {
            type ??= "text/plain";
            blob = new Blob([value], { type: type })
        }
        else {
            type ??= "image/png";
            if (!type.startsWith("image")) {
                return "Only text or image data is allowed.";
            }

            const data: ArrayBuffer = await value.arrayBuffer();
            blob = new Blob([data], { type: type })
        }
        const item: ClipboardItem = new ClipboardItem({ [type]: blob });

        try {
            await navigator.clipboard.write([item])
            return null;
        }
        catch (e: unknown) {
            if (e instanceof Error) {
                if (e.name === "NotAllowed") {
                    return e.name;
                }
                return e.message;
            }

            return "An error occurred."
        }
    }

    export async function writeText(value: string): Promise<string | null> {
        try {
            await navigator.clipboard.writeText(value);
            return null;
        }
        catch (e: unknown) {
            if (e instanceof Error) {
                if (e.name === "NotAllowed") {
                    return e.name;
                }
                return e.message;
            }

            return "An error occurred."
        }
    }
}