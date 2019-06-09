export = logger;
declare function logger(opts: any, stream: any): any;
declare namespace logger {
  const startTime: symbol;
  namespace stdSerializers {
    function err(err: any): any;
    function req(req: any): any;
    function res(res: any): any;
  }
}
