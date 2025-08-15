async function initRecorder(previewEl, startBtn, stopBtn, inputField) {
  if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
    alert('Камера недоступна в этом браузере');
    return;
  }
  const stream = await navigator.mediaDevices.getUserMedia({ video: true, audio: true });
  previewEl.srcObject = stream;
  const chunks = [];
  const rec = new MediaRecorder(stream, { mimeType: 'video/webm;codecs=vp9' });
  rec.ondataavailable = e => { if (e.data && e.data.size > 0) chunks.push(e.data); };
  rec.onstop = () => {
    const blob = new Blob(chunks, { type: 'video/webm' });
    const file = new File([blob], `recording_${Date.now()}.webm`, { type: 'video/webm' });
    const dt = new DataTransfer();
    dt.items.add(file);
    inputField.files = dt.files;
  };
  startBtn.onclick = () => rec.start();
  stopBtn.onclick = () => rec.stop();
}
