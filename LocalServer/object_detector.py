

class ObjectDetector:
    def __init__(self, model_path='ObjectDetectorYOLO.pt'):
        from ultralytics import YOLO
        self.model = YOLO(model_path)
        self.set_parameters()

    def set_parameters(self, conf=0.25, iou=0.45, agnostic_nms=False, max_det=1000):
        self.model.overrides['conf'] = conf
        self.model.overrides['iou'] = iou
        self.model.overrides['agnostic_nms'] = agnostic_nms
        self.model.overrides['max_det'] = max_det

    def predict(self, image):
        results = self.model.predict(image,verbose=False)
        return results