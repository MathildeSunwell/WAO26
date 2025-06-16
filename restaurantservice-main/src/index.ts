// OBSERVABILITY: Initialize OpenTelemetry tracing FIRST
import './config/instrumentation.js';

// CORE DEPENDENCIES
import express from 'express';
import { initializeRabbitMq } from './config/messageBroker.js';
import { consume } from './services/messaging/consumer.js';
import { orderEventHandler } from './services/handlers/orderEventHandler.js';
import { getLogger } from './config/logger/logContext.js'
import { apiRouter, markReady } from './routes/routes.js';
import { CONSUME_QUEUE } from "./config/topology.js";
import { connectMongo } from "./config/database.js";

// ============================================
// MAIN MODULE (INITIAL PHASE) - EVENT LOOP
// ============================================
// Everything below runs SYNCHRONOUSLY in the Initial Phase
// No callbacks are executed yet - only registered!

const logger = getLogger();
const app = express();
const port = process.env.PORT ? parseInt(process.env.PORT) : 3000;

// EXPRESS MIDDLEWARE SETUP
// Configure Express to parse JSON requests
app.use(express.json());

// ROUTE REGISTRATION
// Register all API routes (health checks, etc.)
app.use(apiRouter);

// HTTP SERVER STARTUP
// This registers a callback for when server starts - doesn't execute immediately!
app.listen(port, () => {
   // This callback runs in POLL PHASE when server is ready
   logger.info(`Server listening on http://localhost:${port}`);

   // START ASYNC INITIALIZATION
   // init() returns a Promise - this schedules async work
   init().catch(err => {
       // ERROR HANDLING: If initialization fails, exit process
       logger.error('Initialization failed', err);
       process.exit(1);
   });
});

// ============================================
// ASYNC INITIALIZATION FUNCTION
// ============================================
// This function contains all our async setup operations
// Each await operation will be handled by different event loop phases

async function init() {
   logger.info('Initializing application...');
   
   // DATABASE CONNECTION (POLL PHASE)
   // MongoDB connection - I/O operation handled in Poll Phase
   await connectMongo();
   
   // MESSAGE BROKER SETUP (POLL PHASE)  
   // RabbitMQ connection and queue setup - I/O operation in Poll Phase
   await initializeRabbitMq();
   
   // START MESSAGE CONSUMPTION (POLL PHASE)
   // This sets up the message consumer that will process order events
   // Each incoming message will be handled in Poll Phase
   await consume(CONSUME_QUEUE, async (raw, correlationId) => {
       // BUSINESS LOGIC: Each message triggers order processing
       // This callback runs in POLL PHASE for every RabbitMQ message
       await orderEventHandler(raw, correlationId);
   });
   
   // MARK SERVICE AS READY
   // Signal that health checks should return "ready" status
   markReady();
}

// ============================================
// EVENT LOOP PHASES IN THIS FILE:
// ============================================
// 
// 1. MAIN MODULE (Initial Phase):
//    - All imports and variable declarations
//    - Express setup and route registration
//    - app.listen() call (registers callback)
//
// 2. POLL PHASE (I/O Operations):
//    - Server startup callback execution
//    - MongoDB connection (connectMongo)
//    - RabbitMQ setup (initializeRabbitMq)  
//    - Message consumption setup (consume)
//    - Each incoming HTTP request
//    - Each incoming RabbitMQ message
//
// 3. TIMER PHASE (if any setTimeout):
//    - Would handle retry delays from other modules
//    - Kitchen preparation timers from order handlers
//
// 4. NO setImmediate() used = No Check Phase activity
// 5. NO explicit close handlers = Minimal Close Phase activity
//
// ============================================
// EXAM TALKING POINTS:
// ============================================
//
// KEY CONCEPTS TO EXPLAIN:
// 
// 1. "This file demonstrates the Node.js event loop phases clearly"
// 2. "Initial Phase: All synchronous setup happens first"  
// 3. "Poll Phase: All I/O operations (DB, messaging, HTTP) happen here"
// 4. "Single-threaded but non-blocking: Multiple operations can be 'in progress'"
// 5. "Event-driven architecture: We register callbacks, event loop executes them"
//
// RESTAURANT SERVICE FLOW:
// 1. Service starts (Initial Phase)
// 2. Connections establish (Poll Phase) 
// 3. Orders arrive via RabbitMQ (Poll Phase)
// 4. Database operations process orders (Poll Phase)
// 5. HTTP health checks work concurrently (Poll Phase)
// 6. Everything runs efficiently without blocking!