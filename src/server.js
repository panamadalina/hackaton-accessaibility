const express = require('express');
const favicon = require('serve-favicon');
const path = require('path');
const utils = require('./utils');
const { textToSpeech } = require('./az-cognitive-services/from-text-to-speech');
const { speechToText } = require('./az-cognitive-services/from-speech-to-text');


const key="7288c10f030b4911a55596df1c605599"
const region='eastus'

// creates a temp file on server, the streams to client
/* eslint-disable no-unused-vars */

// fn to create express server
const create = async () => {

    // server
    const app = express();
    app.use(favicon(path.join(__dirname, '../public', 'favicon.ico')));
    
    // Log request
    app.use(utils.appLogger);

    // root route - serve static file
    app.get('/api/hello', (req, res) => {
        res.json({hello: 'goodbye'});
        res.end();
    });

    // root route - serve static file
    app.get('/', (req, res) => {
        return res.sendFile(path.join(__dirname, '../public/client.html'));

    });

    app.get('/text-to-speech', async (req, res, next) => {        
        var phrase=req.query.phrase || "hello world" 
        var language= req.query.language || "ro-Ro-EmilNeural"
       // phrase=req.query.phrase
        if (!key || !region || !phrase) res.status(404).send('Invalid query string');
        
        let fileName = null;

        fileName=`rezultat.mp3`
        const audioStream = await textToSpeech(key, region, phrase,language, fileName);
        res.set({
            'Content-Type': 'audio/mpeg',
            'Transfer-Encoding': 'chunked'
        });
        audioStream.pipe(res);
    });
    app.get('/speech-to-text', async (req, res, next) => { 
        var language= req.query.language || "ro-Ro";
        var audio=req.query.audio //|| "https://speechtotextdemo.blob.core.windows.net/speechtotextcontainer/ro-RO-EmilNeural-2020-10-20T15_00_00_000Z-2020-10-20T15_00_10_000Z.mp3" 
        const textFromSpeech = await speechToText(key, region, audio,language);
        res.set({
            'Content-Type': 'application/json'
           // 'Transfer-Encoding': 'chunked'
        });
        //audioStream.pipe(res);
        res.send(textFromSpeech);
    });

    

    // Catch errors
    app.use(utils.logErrors);
    app.use(utils.clientError404Handler);
    app.use(utils.clientError500Handler);
    app.use(utils.errorHandler);

    return app;
};

module.exports = {
    create
};