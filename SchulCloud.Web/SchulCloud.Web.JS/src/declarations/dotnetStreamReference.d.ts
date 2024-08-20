/**
 * A stream transferred from .NET to js via SignalR.
 */
export declare interface dotNetStreamReference {

    /**
     * Gets the stream as an array buffer.
     */
    arrayBuffer(): Promise<ArrayBuffer>
}