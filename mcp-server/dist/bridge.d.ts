export type RevitBridgeConfig = {
    host: string;
    timeoutMs: number;
};
export declare function resolveRevitBridgeConfig(env?: NodeJS.ProcessEnv): RevitBridgeConfig;
export declare function normalizeRevitHost(rawHost: string, allowRemote?: boolean): string;
export declare function parseTimeoutMs(rawTimeout: string): number;
export declare function validateRevitAction(action: string): string;
export declare function buildRevitApiUrl(host: string, action: string): string;
export declare function revit(action: string, payload?: Record<string, unknown>, config?: RevitBridgeConfig): Promise<unknown>;
export declare function revitHealth(config?: RevitBridgeConfig): Promise<unknown>;
//# sourceMappingURL=bridge.d.ts.map