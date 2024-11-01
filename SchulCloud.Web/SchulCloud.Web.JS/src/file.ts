import { DotNetStreamReference } from "./declarations/dotnetStreamReference";

export namespace File {
    export async function download(streamReference: DotNetStreamReference, fileName: string, mimeType?: string, endings?: EndingType): Promise<void> {
        const arrayBuffer: ArrayBuffer = await streamReference.arrayBuffer();
        const blob: Blob = new Blob([arrayBuffer], {
            type: mimeType,
            endings: endings
        });

        const url = URL.createObjectURL(blob);
        downloadFromUrl(fileName, url);
        URL.revokeObjectURL(url);
    }

    export function downloadFromUrl(fileName: string, url: string): void {
        const anchorElement = document.createElement('a');

        anchorElement.href = url;
        anchorElement.download = fileName;
        anchorElement.target = "_self";

        anchorElement.click();
        anchorElement.remove();
    }
}