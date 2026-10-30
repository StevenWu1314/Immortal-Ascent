#!/usr/bin/env node

const http = require('http');
const { Buffer } = require('buffer');

const UNITY_ENDPOINT = 'http://localhost:57123';

async function fetchUnityTools() {
  return new Promise((resolve, reject) => {
    http.get(`${UNITY_ENDPOINT}/tools`, (res) => {
      let data = '';
      res.on('data', (chunk) => { data += chunk; });
      res.on('end', () => {
        try {
          const parsed = JSON.parse(data);
          resolve(parsed.tools || []);
        } catch (e) {
          reject(new Error(`Failed to parse Unity tools response: ${e.message}`));
        }
      });
    }).on('error', (e) => {
      reject(new Error(`Failed to fetch Unity tools: ${e.message}`));
    });
  });
}

async function callUnityTool(toolName, args) {
  return new Promise((resolve, reject) => {
    const postData = JSON.stringify(args);

    const options = {
      hostname: 'localhost',
      port: 57123,
      path: `/${toolName}`,
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Content-Length': Buffer.byteLength(postData)
      }
    };

    const req = http.request(options, (res) => {
      let data = '';
      res.on('data', (chunk) => { data += chunk; });
      res.on('end', () => {
        try {
          const parsed = JSON.parse(data);
          if (parsed.error) {
            reject(new Error(parsed.error));
          } else {
            resolve(parsed.result || '');
          }
        } catch (e) {
          reject(new Error(`Failed to parse Unity call response: ${e.message}`));
        }
      });
    });

    req.on('error', (e) => {
      reject(new Error(`Failed to call Unity tool: ${e.message}`));
    });

    req.write(postData);
    req.end();
  });
}

class MCPServer {
  constructor() {
    this.tools = [];
  }

  async initialize() {
    try {
      this.tools = await fetchUnityTools();
      this.logError(`Loaded ${this.tools.length} tool(s) from Unity`);
    } catch (e) {
      this.logError(`Warning: Could not load tools from Unity: ${e.message}`);
    }
  }

  logError(message) {
    console.error(message);
  }

  async handleMessage(message) {
    const { jsonrpc, id, method, params } = message;

    if (jsonrpc !== '2.0') {
      return this.createError(id, -32600, 'Invalid Request: jsonrpc must be "2.0"');
    }

    try {
      switch (method) {
        case 'initialize':
          return this.handleInitialize(id, params);
        
        case 'tools/list':
          return this.handleToolsList(id);
        
        case 'tools/call':
          return await this.handleToolsCall(id, params);
        
        case 'notifications/initialized':
          return null;
        
        default:
          return this.createError(id, -32601, `Method not found: ${method}`);
      }
    } catch (error) {
      this.logError(`Error handling ${method}: ${error.message}`);
      return this.createError(id, -32603, `Internal error: ${error.message}`);
    }
  }

  handleInitialize(id, params) {
    return {
      jsonrpc: '2.0',
      id,
      result: {
        protocolVersion: '2024-11-05',
        capabilities: {
          tools: {}
        },
        serverInfo: {
          name: 'unity-agent-client-builtin-mcp-server',
          version: '0.1.0'
        }
      }
    };
  }

  handleToolsList(id) {
    return {
      jsonrpc: '2.0',
      id,
      result: {
        tools: this.tools
      }
    };
  }

  async handleToolsCall(id, params) {
    const { name, arguments: args } = params;

    if (!name) {
      return this.createError(id, -32602, 'Invalid params: name is required');
    }

    try {
      const result = await callUnityTool(name, args || {});
      
      return {
        jsonrpc: '2.0',
        id,
        result: {
          content: [
            {
              type: 'text',
              text: result
            }
          ]
        }
      };
    } catch (error) {
      this.logError(`Error calling tool ${name}: ${error.message}`);
      return this.createError(id, -32603, `Tool execution failed: ${error.message}`);
    }
  }

  createError(id, code, message) {
    return {
      jsonrpc: '2.0',
      id,
      error: {
        code,
        message
      }
    };
  }
}

class StdioTransport {
  constructor(server) {
    this.server = server;
    this.buffer = '';
  }

  start() {
    process.stdin.setEncoding('utf8');
    process.stdin.on('data', (chunk) => this.handleData(chunk));
    process.stdin.on('end', () => process.exit(0));
    
    this.server.logError('Unity MCP Server started on stdio');
  }

  handleData(chunk) {
    this.buffer += chunk;
    
    const lines = this.buffer.split('\n');
    this.buffer = lines.pop() || '';
    
    for (const line of lines) {
      if (line.trim()) {
        this.processMessage(line.trim());
      }
    }
  }

  async processMessage(line) {
    try {
      const message = JSON.parse(line);
      const response = await this.server.handleMessage(message);
      
      if (response) {
        this.send(response);
      }
    } catch (error) {
      this.server.logError(`Failed to process message: ${error.message}`);
      this.server.logError(`Message: ${line}`);
    }
  }

  send(message) {
    const json = JSON.stringify(message);
    process.stdout.write(json + '\n');
  }
}

async function main() {
  const server = new MCPServer();
  await server.initialize();
  
  const transport = new StdioTransport(server);
  transport.start();
}

process.on('uncaughtException', (error) => {
  console.error('Uncaught exception:', error);
  process.exit(1);
});

process.on('unhandledRejection', (reason, promise) => {
  console.error('Unhandled rejection at:', promise, 'reason:', reason);
  process.exit(1);
});

main().catch((error) => {
  console.error('Failed to start server:', error);
  process.exit(1);
});
