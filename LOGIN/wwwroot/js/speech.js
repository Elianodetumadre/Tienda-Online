let recognition;

if ('webkitSpeechRecognition' in window) {

    recognition = new webkitSpeechRecognition();

    recognition.lang = 'es-ES';
    recognition.continuous = true;
    recognition.interimResults = true;

    recognition.onresult = function (event) {

        let textoFinal = '';

        for (let i = event.resultIndex; i < event.results.length; i++) {

            textoFinal += event.results[i][0].transcript;
        }

        document.getElementById("textoReconocido").value = textoFinal;
    };

    recognition.onerror = function (event) {
        console.log(event.error);
    };
}

document.getElementById("btnHablar")
    .addEventListener("click", () => {

        if (recognition) {
            recognition.start();
        }
    });

document.getElementById("btnDetener")
    .addEventListener("click", () => {

        if (recognition) {
            recognition.stop();
        }
    });